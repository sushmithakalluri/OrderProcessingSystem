namespace InventoryService.Entities;

public class Product
{
    public Guid ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public int StockQuantity { get; set; }

    public decimal Price { get; set; }

    public DateTime UpdatedAt { get; set; }
}