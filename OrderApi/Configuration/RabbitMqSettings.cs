namespace OrderApi.Configuration;

public class RabbitMqSettings
{
    public string HostName { get; set; } = string.Empty;

    public string UserName { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string ExchangeName { get; set; } = string.Empty;
    
    public string ResultExchangeName { get; set; } = string.Empty;

    public string ResultQueueName { get; set; } = string.Empty;
}