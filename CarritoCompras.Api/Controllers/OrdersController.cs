using System.Security.Claims;
using CarritoCompras.Domain.Common;
using CarritoCompras.Domain.DTOs.Orders;
using CarritoCompras.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarritoCompras.Api.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    private int GetUserId()
    {
        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.Parse(idClaim!);
    }

    [HttpPost]
    public async Task<IActionResult> Checkout()
    {
        var result = await _orderService.CheckoutAsync(GetUserId());

        if (result.Success)
        {
            return Ok(result.Data);
        }

        return result.ErrorType switch
        {
            ServiceErrorType.Conflict => Conflict(new { message = result.ErrorMessage }),
            _ => BadRequest(new { message = result.ErrorMessage })
        };
    }

    [HttpGet]
    public async Task<IActionResult> GetHistory()
    {
        var history = await _orderService.GetOrderHistoryAsync(GetUserId());
        return Ok(history);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetDetail(int id)
    {
        var order = await _orderService.GetOrderDetailAsync(id, GetUserId());

        if (order is null)
        {
            return NotFound(new { message = $"Orden con id {id} no encontrada." });
        }

        return Ok(order);
    }
}