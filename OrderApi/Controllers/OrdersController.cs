using Microsoft.AspNetCore.Mvc;
using OrderApi.DTOs;
using OrderApi.Exceptions;
using OrderApi.Services;

namespace OrderApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder(CreateOrderRequest request)
    {
        try
        {
            var response = await _orderService.CreateOrderAsync(request);
            return Accepted(response);
        }
        catch (OrderValidationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}