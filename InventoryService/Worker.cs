using System.Text;
using System.Text.Json;
using InventoryService.Configuration;
using InventoryService.Data;
using InventoryService.DTOs;
using InventoryService.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace InventoryService;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly RabbitMqSettings _settings;
    private readonly IServiceScopeFactory _scopeFactory;

    public Worker(
        ILogger<Worker> logger,
        IOptions<RabbitMqSettings> options,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _settings = options.Value;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _settings.HostName,
            UserName = _settings.UserName,
            Password = _settings.Password
        };

        IConnection? connection = null;
        IChannel? channel = null;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                connection = await factory.CreateConnectionAsync(stoppingToken);
                channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);
                _logger.LogInformation("InventoryService connected to RabbitMQ.");
                break;
            }   
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "RabbitMQ is not ready. InventoryService retrying in 5 seconds...");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
        
        if (channel == null)
        {
            return;
        }

        await channel.ExchangeDeclareAsync(
            exchange: _settings.ExchangeName,
            type: ExchangeType.Fanout,
            durable: true,
            cancellationToken: stoppingToken);

        await channel.QueueDeclareAsync(
            queue: _settings.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: stoppingToken);

        await channel.QueueBindAsync(
            queue: _settings.QueueName,
            exchange: _settings.ExchangeName,
            routingKey: string.Empty,
            cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.ReceivedAsync += async (sender, eventArgs) =>
        {
            try
            {
                var body = eventArgs.Body.ToArray();
                var json = Encoding.UTF8.GetString(body);

                var orderCreatedEvent = JsonSerializer.Deserialize<OrderCreatedEvent>(
                    json,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                if (orderCreatedEvent == null)
                {
                    _logger.LogWarning("Received invalid order.created message");

                    await channel.BasicNackAsync(
                        deliveryTag: eventArgs.DeliveryTag,
                        multiple: false,
                        requeue: false);

                    return;
                }

                using var scope = _scopeFactory.CreateScope();

                var dbContext = scope.ServiceProvider
                    .GetRequiredService<InventoryDbContext>();

                var publisher = scope.ServiceProvider
                    .GetRequiredService<RabbitMqPublisher>();

                var isInventoryAvailable = true;
                var failureReason = string.Empty;

                foreach (var item in orderCreatedEvent.Items)
                {
                    var product = await dbContext.Products
                        .FirstOrDefaultAsync(
                            p => p.ProductId == item.ProductId,
                            stoppingToken);

                    if (product == null)
                    {
                        isInventoryAvailable = false;
                        failureReason = $"Product not found: {item.ProductId}";
                        break;
                    }

                    if (product.StockQuantity < item.Quantity)
                    {
                        isInventoryAvailable = false;
                        failureReason =
                            $"Insufficient stock for product {product.ProductName}. Available: {product.StockQuantity}, Requested: {item.Quantity}";
                        break;
                    }

                    product.StockQuantity -= item.Quantity;
                    product.UpdatedAt = DateTime.UtcNow;

                    _logger.LogInformation(
                        "Stock updated. Product: {ProductName}, Remaining Stock: {Stock}",
                        product.ProductName,
                        product.StockQuantity);
                }

                string resultStatus;

                if (isInventoryAvailable)
                {
                    await dbContext.SaveChangesAsync(stoppingToken);
                    resultStatus = "Confirmed";
                }
                else
                {
                    resultStatus = "Failed";
                }

                var resultEvent = new OrderInventoryResultEvent
                {
                    OrderId = orderCreatedEvent.OrderId,
                    Status = resultStatus,
                    Reason = failureReason
                };

                var resultJson = JsonSerializer.Serialize(resultEvent);

                await publisher.PublishResultAsync(resultJson);

                _logger.LogInformation(
                    "Inventory result published. OrderId: {OrderId}, Status: {Status}, Reason: {Reason}",
                    resultEvent.OrderId,
                    resultEvent.Status,
                    resultEvent.Reason);

                await channel.BasicAckAsync(
                    deliveryTag: eventArgs.DeliveryTag,
                    multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing inventory message");

                await channel.BasicNackAsync(
                    deliveryTag: eventArgs.DeliveryTag,
                    multiple: false,
                    requeue: true);
            }
        };

        await channel.BasicConsumeAsync(
            queue: _settings.QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        _logger.LogInformation(
            "InventoryService is listening to queue: {QueueName}",
            _settings.QueueName);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}