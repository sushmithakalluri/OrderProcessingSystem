using System.Text;
using System.Text.Json;
using InventoryService.Configuration;
using InventoryService.DTOs;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using InventoryService.Data;
using Microsoft.EntityFrameworkCore;

namespace InventoryService;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly RabbitMqSettings _settings;
    private readonly IServiceScopeFactory _scopeFactory;

    public Worker(
        ILogger<Worker> logger,
        IOptions<RabbitMqSettings> options, IServiceScopeFactory scopeFactory)
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

        var connection = await factory.CreateConnectionAsync(stoppingToken);
        var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

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
            
            foreach (var item in orderCreatedEvent.Items)
            {
                var product = await dbContext.Products
                    .FirstOrDefaultAsync(p => p.ProductId == item.ProductId);

                if (product == null)    
                {
                    _logger.LogWarning("Product not found: {ProductId}", item.ProductId);
                    continue;
                }

                if (product.StockQuantity < item.Quantity)
                {
                    _logger.LogWarning(
                        "Insufficient stock for product {ProductId}. Available: {Available}, Requested: {Requested}",
                        product.ProductId,
                        product.StockQuantity,
                        item.Quantity);
                    continue;
                }

                product.StockQuantity -= item.Quantity;
                _logger.LogInformation(
                    "Stock updated. Product: {ProductName}, Remaining Stock: {Stock}",
                    product.ProductName,
                    product.StockQuantity);
            }

            await dbContext.SaveChangesAsync();
            _logger.LogInformation(
                "Inventory processing completed for OrderId: {OrderId}",
                orderCreatedEvent.OrderId);
            
            await channel.BasicAckAsync(
                deliveryTag: eventArgs.DeliveryTag,
                multiple: false);
    
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