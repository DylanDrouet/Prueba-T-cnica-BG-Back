using CarritoCompras.Domain.Entities;

namespace CarritoCompras.Domain.Interfaces;

public interface IOrderRepository
{
    Task<Order> CreateOrderAsync(Order order);
    Task<List<Order>> GetOrdersByUserAsync(int userId);
    Task<Order?> GetOrderByIdAsync(int orderId, int userId);
}