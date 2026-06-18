namespace OrderApi.Messaging;

public interface IRabbitMqPublisher
{
    Task PublishAsync(string message);
}