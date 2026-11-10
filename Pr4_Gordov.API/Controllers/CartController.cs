using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pr4_Gordov.API.DTO.Cart;
using Pr4_Gordov.API.Services;

namespace Pr4_Gordov.API.Controllers;

[ApiController]
[Route("api/cart")]
[Authorize]
public class CartController : ControllerBase
{
    private readonly CartService _cartService;

    public CartController(CartService cartService)
    {
        _cartService = cartService;
    }

    private int GetCurrentUserId()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.Parse(idClaim!);
    }

    [HttpGet]
    public async Task<IActionResult> GetCart()
    {
        var cart = await _cartService.GetCartAsync(GetCurrentUserId());
        return Ok(cart);
    }

    [HttpPost]
    public async Task<IActionResult> AddToCart([FromBody] AddToCartRequest request)
    {
        var (success, errorMessage) = await _cartService.AddToCartAsync(GetCurrentUserId(), request);
        if (!success)
            return BadRequest(new { message = errorMessage });

        return Ok(new { message = "Товар добавлен в корзину" });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateQuantity(int id, [FromBody] UpdateCartItemRequest request)
    {
        var (success, errorMessage) = await _cartService.UpdateQuantityAsync(GetCurrentUserId(), id, request.Quantity);
        if (!success)
            return BadRequest(new { message = errorMessage });

        return Ok(new { message = "Количество обновлено" });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Remove(int id)
    {
        bool success = await _cartService.RemoveFromCartAsync(GetCurrentUserId(), id);
        if (!success)
            return NotFound(new { message = "Позиция не найдена в корзине" });

        return Ok(new { message = "Товар удален из корзины" });
    }
}