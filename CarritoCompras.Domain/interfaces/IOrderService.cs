using CarritoCompras.Domain.Common;
using CarritoCompras.Domain.DTOs.Orders;

namespace CarritoCompras.Domain.Interfaces;

public interface IOrderService
{
    Task<ServiceResult<OrderDto>> CheckoutAsync(int userId);
    Task<List<OrderSummaryDto>> GetOrderHistoryAsync(int userId);
    Task<OrderDto?> GetOrderDetailAsync(int orderId, int userId);
}