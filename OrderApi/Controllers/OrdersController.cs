using Microsoft.AspNetCore.Mvc;
using OrderApi.Data;
using OrderApi.DTOs;
using OrderApi.Entities;

namespace OrderApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly OrderDbContext _context;

    public OrdersController(OrderDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder(CreateOrderRequest request)
    {
        var now = DateTime.UtcNow;

        var order = new Order
        {
            OrderId = Guid.NewGuid(),
            CustomerId = request.CustomerId,
            CustomerEmail = request.CustomerEmail,
            TotalAmount = request.TotalAmount,
            Status = "Pending",
            CreatedAt = now,
            UpdatedAt = now
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        return Accepted(new
        {
            order.OrderId,
            order.Status,
            Message = "Order received and is being processed"
        });
    }
}