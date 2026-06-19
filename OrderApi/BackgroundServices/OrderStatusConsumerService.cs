using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OrderApi.Configuration;
using OrderApi.Data;
using OrderApi.DTOs;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace OrderApi.BackgroundServices;

public class OrderStatusConsumerService : BackgroundService
{
    private readonly RabbitMqSettings _settings;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OrderStatusConsumerService> _logger;

    public OrderStatusConsumerService(
        IOptions<RabbitMqSettings> options,
        IServiceScopeFactory scopeFactory,
        ILogger<OrderStatusConsumerService> logger)
    {
        _settings = options.Value;
        _scopeFactory = scopeFactory;
        _logger = logger;
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
            exchange: _settings.ResultExchangeName,
            type: ExchangeType.Fanout,
            durable: true,
            cancellationToken: stoppingToken);

        await channel.QueueDeclareAsync(
            queue: _settings.ResultQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: stoppingToken);

        await channel.QueueBindAsync(
            queue: _settings.ResultQueueName,
            exchange: _settings.ResultExchangeName,
            routingKey: string.Empty,
            cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.ReceivedAsync += async (sender, eventArgs) =>
        {
            try
            {
                var body = eventArgs.Body.ToArray();
                var json = Encoding.UTF8.GetString(body);

                var resultEvent = JsonSerializer.Deserialize<OrderInventoryResultEvent>(
                    json,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                if (resultEvent == null)
                {
                    _logger.LogWarning("Received invalid inventory result message");

                    await channel.BasicNackAsync(
                        deliveryTag: eventArgs.DeliveryTag,
                        multiple: false,
                        requeue: false);

                    return;
                }

                using var scope = _scopeFactory.CreateScope();

                var dbContext = scope.ServiceProvider
                    .GetRequiredService<OrderDbContext>();

                var order = await dbContext.Orders
                    .FirstOrDefaultAsync(
                        o => o.OrderId == resultEvent.OrderId,
                        stoppingToken);

                if (order == null)
                {
                    _logger.LogWarning(
                        "Order not found for inventory result. OrderId: {OrderId}",
                        resultEvent.OrderId);

                    await channel.BasicNackAsync(
                        deliveryTag: eventArgs.DeliveryTag,
                        multiple: false,
                        requeue: false);

                    return;
                }

                order.Status = resultEvent.Status;
                order.UpdatedAt = DateTime.UtcNow;

                await dbContext.SaveChangesAsync(stoppingToken);

                _logger.LogInformation(
                    "Order status updated. OrderId: {OrderId}, Status: {Status}, Reason: {Reason}",
                    resultEvent.OrderId,
                    resultEvent.Status,
                    resultEvent.Reason);

                await channel.BasicAckAsync(
                    deliveryTag: eventArgs.DeliveryTag,
                    multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing inventory result message");

                await channel.BasicNackAsync(
                    deliveryTag: eventArgs.DeliveryTag,
                    multiple: false,
                    requeue: true);
            }
        };

        await channel.BasicConsumeAsync(
            queue: _settings.ResultQueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        _logger.LogInformation(
            "OrderApi is listening to inventory result queue: {QueueName}",
            _settings.ResultQueueName);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}