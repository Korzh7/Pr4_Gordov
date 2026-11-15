using Microsoft.EntityFrameworkCore;
using Pr4_Gordov.API.Data;
using Pr4_Gordov.API.DTO.Cart;
using Pr4_Gordov.API.DTO.Favorites;
using Pr4_Gordov.API.Models;

namespace Pr4_Gordov.API.Services;

public class FavoriteService
{
    private readonly AppDbContext _context;
    private readonly CartService _cartService;

    public FavoriteService(AppDbContext context, CartService cartService)
    {
        _context = context;
        _cartService = cartService;
    }

    public async Task<List<FavoriteItemResponse>> GetFavoritesAsync(int userId)
    {
        return await _context.Favorites
            .AsNoTracking()
            .Where(f => f.UserId == userId)
            .Include(f => f.Product)
            .ThenInclude(p => p.Category)
            .Select(f => new FavoriteItemResponse(
                f.Id,
                f.ProductId,
                f.Product.Title,
                f.Product.Price,
                f.Product.ImagePath,
                f.Product.Category.Name,
                f.Product.IsAvailable,
                f.Product.StockQuantity
            ))
            .ToListAsync();
    }

    public async Task<(bool Success, string? ErrorMessage)> AddFavoriteAsync(int userId, int productId)
    {
        var product = await _context.Products.FindAsync(productId);
        if (product == null)
            return (false, "Товар не найден");

        bool exists = await _context.Favorites
            .AnyAsync(f => f.UserId == userId && f.ProductId == productId);

        if (exists)
            return (false, "Товар уже находится в избранном");

        var favorite = new Favorite
        {
            UserId = userId,
            ProductId = productId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Favorites.Add(favorite);
        await _context.SaveChangesAsync();

        return (true, null);
    }

    public async Task<bool> RemoveFavoriteAsync(int userId, int productId)
    {
        var favorite = await _context.Favorites
            .FirstOrDefaultAsync(f => f.UserId == userId && f.ProductId == productId);

        if (favorite == null)
            return false;

        _context.Favorites.Remove(favorite);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<(bool Success, string? ErrorMessage)> MoveToCartAsync(int userId, int productId)
    {
        var addResult = await _cartService.AddToCartAsync(userId, new AddToCartRequest(productId, 1));
        if (!addResult.Success)
            return addResult;

        await RemoveFavoriteAsync(userId, productId);
        return (true, null);
    }
}