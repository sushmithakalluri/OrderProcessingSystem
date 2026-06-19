namespace InventoryService.DTOs;

public class OrderCreatedEvent
{
    public Guid OrderId { get; set; }

    public string CustomerId { get; set; } = string.Empty;

    public string CustomerEmail { get; set; } = string.Empty;

    public List<OrderCreatedItem> Items { get; set; } = new();

    public decimal TotalAmount { get; set; }
}

public class OrderCreatedItem
{
    public Guid ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }
}