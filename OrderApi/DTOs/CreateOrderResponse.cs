namespace OrderApi.DTOs;

public class CreateOrderResponse
{
    public Guid OrderId { get; set; }

    public string Status { get; set; } = string.Empty;

    public decimal TotalAmount { get; set; }

    public string Message { get; set; } = string.Empty;
}