using OrderQueryService.DTOs;

namespace OrderQueryService.Services;

public interface IOrderQueryService
{
    Task<OrderResponse?> GetOrderByIdAsync(Guid orderId);
}