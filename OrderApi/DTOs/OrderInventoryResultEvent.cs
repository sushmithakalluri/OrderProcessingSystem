namespace OrderApi.DTOs;

public class OrderInventoryResultEvent
{
    public Guid OrderId { get; set; }

    public string Status { get; set; } = string.Empty;

    public string Reason { get; set; } = string.Empty;
}