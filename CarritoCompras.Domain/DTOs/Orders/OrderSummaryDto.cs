namespace CarritoCompras.Domain.DTOs.Orders;

public class OrderSummaryDto
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public decimal Total { get; set; }
    public int ItemCount { get; set; }
}