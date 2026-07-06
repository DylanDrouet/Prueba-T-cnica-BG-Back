using CarritoCompras.Domain.Common;
using CarritoCompras.Domain.DTOs.Orders;
using CarritoCompras.Domain.Entities;
using CarritoCompras.Domain.Interfaces;
using CarritoCompras.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CarritoCompras.Infrastructure.Services;

public class OrderService : IOrderService
{
    private readonly AppDbContext _context;
    private readonly ICartRepository _cartRepository;
    private readonly IOrderRepository _orderRepository;

    public OrderService(AppDbContext context, ICartRepository cartRepository, IOrderRepository orderRepository)
    {
        _context = context;
        _cartRepository = cartRepository;
        _orderRepository = orderRepository;
    }

    public async Task<ServiceResult<OrderDto>> CheckoutAsync(int userId)
    {
        var cart = await _cartRepository.GetCartWithItemsAsync(userId);

        if (cart is null || cart.Items.Count == 0)
        {
            return ServiceResult<OrderDto>.Fail("El carrito está vacío.", ServiceErrorType.Validation);
        }

        foreach (var item in cart.Items)
        {
            if (item.Quantity > item.Product.Stock)
            {
                return ServiceResult<OrderDto>.Fail(
                    $"Stock insuficiente para '{item.Product.Name}'. Disponible: {item.Product.Stock}.",
                    ServiceErrorType.Conflict);
            }
        }

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var subtotal = cart.Items.Sum(i => i.Product.Price * i.Quantity);
            var discountAmount = subtotal > 100 ? Math.Round(subtotal * 0.10m, 2) : 0;

            var order = new Order
            {
                UserId = userId,
                Subtotal = subtotal,
                DiscountAmount = discountAmount,
                Total = subtotal - discountAmount,
                Items = cart.Items.Select(i => new OrderItem
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    UnitPrice = i.Product.Price
                }).ToList()
            };

            _context.Orders.Add(order);

            foreach (var item in cart.Items)
            {
                item.Product.Stock -= item.Quantity;
            }

            cart.Items.Clear();

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return ServiceResult<OrderDto>.Ok(MapToDto(order));
        }
        catch
        {
            await transaction.RollbackAsync();
            return ServiceResult<OrderDto>.Fail("Ocurrió un error al procesar la compra.", ServiceErrorType.Validation);
        }
    }

    public async Task<List<OrderSummaryDto>> GetOrderHistoryAsync(int userId)
    {
        var orders = await _orderRepository.GetOrdersByUserAsync(userId);

        return orders.Select(o => new OrderSummaryDto
        {
            Id = o.Id,
            CreatedAt = o.CreatedAt,
            Total = o.Total,
            ItemCount = o.Items.Sum(i => i.Quantity)
        }).ToList();
    }

    public async Task<OrderDto?> GetOrderDetailAsync(int orderId, int userId)
    {
        var order = await _orderRepository.GetOrderByIdAsync(orderId, userId);
        return order is null ? null : MapToDto(order);
    }

    private static OrderDto MapToDto(Order order)
    {
        return new OrderDto
        {
            Id = order.Id,
            CreatedAt = order.CreatedAt,
            Subtotal = order.Subtotal,
            DiscountAmount = order.DiscountAmount,
            Total = order.Total,
            Items = order.Items.Select(i => new OrderItemDto
            {
                ProductId = i.ProductId,
                ProductName = i.Product?.Name ?? string.Empty,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList()
        };
    }
}