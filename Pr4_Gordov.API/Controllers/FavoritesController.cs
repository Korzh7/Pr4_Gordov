using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pr4_Gordov.API.DTO.Favorites;
using Pr4_Gordov.API.Services;

namespace Pr4_Gordov.API.Controllers;

[ApiController]
[Route("api/favorites")]
[Authorize]
public class FavoritesController : ControllerBase
{
    private readonly FavoriteService _favoriteService;

    public FavoritesController(FavoriteService favoriteService)
    {
        _favoriteService = favoriteService;
    }

    private int GetCurrentUserId()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.Parse(idClaim!);
    }

    [HttpGet]
    public async Task<IActionResult> GetFavorites()
    {
        var items = await _favoriteService.GetFavoritesAsync(GetCurrentUserId());
        return Ok(items);
    }

    [HttpPost]
    public async Task<IActionResult> Add([FromBody] AddFavoriteRequest request)
    {
        var (success, errorMessage) = await _favoriteService.AddFavoriteAsync(GetCurrentUserId(), request.ProductId);
        if (!success)
            return BadRequest(new { message = errorMessage });

        return Ok(new { message = "Товар добавлен в избранное" });
    }

    [HttpDelete("{productId}")]
    public async Task<IActionResult> Remove(int productId)
    {
        bool success = await _favoriteService.RemoveFavoriteAsync(GetCurrentUserId(), productId);
        if (!success)
            return NotFound(new { message = "Товар не найден в избранном" });

        return Ok(new { message = "Товар удален из избранного" });
    }

    [HttpPost("{productId}/move-to-cart")]
    public async Task<IActionResult> MoveToCart(int productId)
    {
        var (success, errorMessage) = await _favoriteService.MoveToCartAsync(GetCurrentUserId(), productId);
        if (!success)
            return BadRequest(new { message = errorMessage });

        return Ok(new { message = "Товар успешно перенесен в корзину" });
    }
}