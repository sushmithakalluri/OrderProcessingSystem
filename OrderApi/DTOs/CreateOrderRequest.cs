namespace OrderApi.DTOs;

public class CreateOrderRequest
{
    public string CustomerId { get; set; } = string.Empty;

    public string CustomerEmail { get; set; } = string.Empty;

    public decimal TotalAmount { get; set; }
}