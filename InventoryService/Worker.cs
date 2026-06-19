using System.Text;
using System.Text.Json;
using InventoryService.Configuration;
using InventoryService.DTOs;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace InventoryService;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly RabbitMqSettings _settings;

    public Worker(
        ILogger<Worker> logger,
        IOptions<RabbitMqSettings> options)
    {
        _logger = logger;
        _settings = options.Value;
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
                return;
            }

            _logger.LogInformation(
                "InventoryService received order.created event. OrderId: {OrderId}, TotalAmount: {TotalAmount}, ItemsCount: {ItemsCount}",
                orderCreatedEvent.OrderId,
                orderCreatedEvent.TotalAmount,
                orderCreatedEvent.Items.Count);

            await Task.CompletedTask;
        };

        await channel.BasicConsumeAsync(
            queue: _settings.QueueName,
            autoAck: true,
            consumer: consumer,
            cancellationToken: stoppingToken);

        _logger.LogInformation(
            "InventoryService is listening to queue: {QueueName}",
            _settings.QueueName);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}