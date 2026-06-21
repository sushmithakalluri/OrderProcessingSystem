using Microsoft.AspNetCore.Mvc;
using OrderQueryService.Services;

namespace OrderQueryService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderQueryService _orderQueryService;

    public OrdersController(IOrderQueryService orderQueryService)
    {
        _orderQueryService = orderQueryService;
    }

    [HttpGet("{orderId:guid}")]
    public async Task<IActionResult> GetOrderById(Guid orderId)
    {
        var order = await _orderQueryService.GetOrderByIdAsync(orderId);

        if (order == null)
        {
            return NotFound(new
            {
                message = "Order not found"
            });
        }

        return Ok(order);
    }
}