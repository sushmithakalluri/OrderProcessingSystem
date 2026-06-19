using System.Text;
using InventoryService.Configuration;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace InventoryService.Messaging;

public class RabbitMqPublisher
{
    private readonly RabbitMqSettings _settings;

    public RabbitMqPublisher(IOptions<RabbitMqSettings> options)
    {
        _settings = options.Value;
    }

    public async Task PublishResultAsync(string message)
    {
        var factory = new ConnectionFactory
        {
            HostName = _settings.HostName,
            UserName = _settings.UserName,
            Password = _settings.Password
        };

        await using var connection = await factory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();

        await channel.ExchangeDeclareAsync(
            exchange: _settings.ResultExchangeName,
            type: ExchangeType.Fanout,
            durable: true);

        var body = Encoding.UTF8.GetBytes(message);

        await channel.BasicPublishAsync(
            exchange: _settings.ResultExchangeName,
            routingKey: string.Empty,
            body: body);
    }
}