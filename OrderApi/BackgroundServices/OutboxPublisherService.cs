using Microsoft.EntityFrameworkCore;
using OrderApi.Data;
using OrderApi.Messaging;

namespace OrderApi.BackgroundServices;

public class OutboxPublisherService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxPublisherService> _logger;

    public OutboxPublisherService(
        IServiceScopeFactory scopeFactory,
        ILogger<OutboxPublisherService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();

                var context =
                    scope.ServiceProvider.GetRequiredService<OrderDbContext>();

                var publisher =
                    scope.ServiceProvider.GetRequiredService<IRabbitMqPublisher>();

                var pendingMessages = await context.OutboxMessages
                    .Where(x => x.Status == "Pending")
                    .ToListAsync(stoppingToken);

                foreach (var message in pendingMessages)
                {
                    await publisher.PublishAsync(message.Payload);

                    message.Status = "Processed";
                    message.ProcessedAt = DateTime.UtcNow;
                }

                await context.SaveChangesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error processing outbox messages");
            }

            await Task.Delay(
                TimeSpan.FromSeconds(5),
                stoppingToken);
        }
    }
}