using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OrderQueryService.Data;
using OrderQueryService.DTOs;
using StackExchange.Redis;

namespace OrderQueryService.Services;

public class OrderQueryService : IOrderQueryService
{
    private readonly OrderQueryDbContext _context;
    private readonly IDatabase _cache;
    private readonly ILogger<OrderQueryService> _logger;

    public OrderQueryService(
        OrderQueryDbContext context,
        IConnectionMultiplexer redis,
        ILogger<OrderQueryService> logger)
    {
        _context = context;
        _cache = redis.GetDatabase();
        _logger = logger;
    }

    public async Task<OrderResponse?> GetOrderByIdAsync(Guid orderId)
    {
        var cacheKey = $"order:{orderId}";

        var cachedOrder = await _cache.StringGetAsync(cacheKey);

        if (!cachedOrder.IsNullOrEmpty)
        {
            _logger.LogInformation("Order returned from Redis cache. OrderId: {OrderId}", orderId);

            return JsonSerializer.Deserialize<OrderResponse>(cachedOrder!);
        }

        _logger.LogInformation("Order not found in Redis. Reading from SQL Server. OrderId: {OrderId}", orderId);

        var order = await _context.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.OrderId == orderId);

        if (order == null)
        {
            return null;
        }

        var response = new OrderResponse
        {
            OrderId = order.OrderId,
            CustomerId = order.CustomerId,
            CustomerEmail = order.CustomerEmail,
            TotalAmount = order.TotalAmount,
            Status = order.Status,
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt
        };

        await _cache.StringSetAsync(
            cacheKey,
            JsonSerializer.Serialize(response),
            TimeSpan.FromMinutes(5));

        _logger.LogInformation("Order stored in Redis cache. OrderId: {OrderId}", orderId);

        return response;
    }
}