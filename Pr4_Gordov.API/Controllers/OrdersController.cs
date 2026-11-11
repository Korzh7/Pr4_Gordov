using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pr4_Gordov.API.DTO.Orders;
using Pr4_Gordov.API.Services;

namespace Pr4_Gordov.API.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly OrderService _orderService;

    public OrdersController(OrderService orderService)
    {
        _orderService = orderService;
    }

    private int GetCurrentUserId()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.Parse(idClaim!);
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        var (success, errorMessage, orderId) = await _orderService.CreateOrderAsync(GetCurrentUserId(), request);
        if (!success)
            return BadRequest(new { message = errorMessage });

        return Ok(new
        {
            orderId,
            message = "Заказ успешно оформлен!"
        });
    }

    [HttpGet]
    public async Task<IActionResult> GetMyOrders([FromQuery] string? sortBy = "newest")
    {
        var orders = await _orderService.GetUserOrdersAsync(GetCurrentUserId(), sortBy);
        return Ok(orders);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrderById(int id)
    {
        var order = await _orderService.GetOrderByIdAsync(GetCurrentUserId(), id);
        if (order == null)
            return NotFound(new { message = "Заказ не найден" });

        return Ok(order);
    }
}