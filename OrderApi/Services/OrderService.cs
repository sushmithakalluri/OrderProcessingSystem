using OrderApi.Data;
using OrderApi.DTOs;
using OrderApi.Entities;
using OrderApi.Exceptions;

namespace OrderApi.Services;

public class OrderService : IOrderService
{
    private readonly OrderDbContext _context;

    public OrderService(OrderDbContext context)
    {
        _context = context;
    }

    public async Task<CreateOrderResponse> CreateOrderAsync(CreateOrderRequest request)
    {
        ValidateRequest(request);

        var now = DateTime.UtcNow;
        var orderId = Guid.NewGuid();

        var orderItems = new List<OrderItem>();
        decimal totalAmount = 0;

        foreach (var item in request.Items)
        {
            if (item.ProductId == Guid.Empty)
            {
                throw new OrderValidationException("ProductId is required.");
            }

            if (item.Quantity <= 0)
            {
                throw new OrderValidationException("Quantity must be greater than zero.");
            }

            var product = await _context.Products.FindAsync(item.ProductId);

            if (product == null)
            {
                throw new OrderValidationException($"Product not found: {item.ProductId}");
            }

            totalAmount += product.Price * item.Quantity;

            orderItems.Add(new OrderItem
            {
                OrderItemId = Guid.NewGuid(),
                OrderId = orderId,
                ProductId = product.ProductId,
                ProductName = product.ProductName,
                Quantity = item.Quantity,
                UnitPrice = product.Price
            });
        }

        var order = new Order
        {
            OrderId = orderId,
            CustomerId = request.CustomerId.Trim(),
            CustomerEmail = request.CustomerEmail.Trim(),
            TotalAmount = totalAmount,
            Status = "Pending",
            CreatedAt = now,
            UpdatedAt = now
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        _context.OrderItems.AddRange(orderItems);
        await _context.SaveChangesAsync();
        
        return new CreateOrderResponse
        {
            OrderId = order.OrderId,
            Status = order.Status,
            TotalAmount = order.TotalAmount,
            Message = "Order received and is being processed"
        };
    }

    private static void ValidateRequest(CreateOrderRequest request)
    {
        if (request == null)
        {
            throw new OrderValidationException("Order request cannot be null.");
        }

        if (string.IsNullOrWhiteSpace(request.CustomerId))
        {
            throw new OrderValidationException("CustomerId is required.");
        }

        if (string.IsNullOrWhiteSpace(request.CustomerEmail))
        {
            throw new OrderValidationException("CustomerEmail is required.");
        }

        if (request.Items == null || request.Items.Count == 0)
        {
            throw new OrderValidationException("Order must contain at least one item.");
        }
    }
}