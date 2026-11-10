using Microsoft.EntityFrameworkCore;
using Pr4_Gordov.API.Data;
using Pr4_Gordov.API.DTO.Cart;
using Pr4_Gordov.API.Models;

namespace Pr4_Gordov.API.Services;

public class CartService
{
    private readonly AppDbContext _context;

    public CartService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<CartResponse> GetCartAsync(int userId)
    {
        var items = await _context.CartItems
            .AsNoTracking()
            .Where(c => c.UserId == userId)
            .Include(c => c.Product)
            .Select(c => new CartItemResponse(
                c.Id,
                c.ProductId,
                c.Product.Title,
                c.Product.Price,
                c.Product.ImagePath,
                c.Quantity,
                c.Product.StockQuantity,
                c.Quantity * c.Product.Price
            ))
            .ToListAsync();

        decimal totalAmount = items.Sum(i => i.TotalPrice);

        return new CartResponse(items, totalAmount);
    }

    public async Task<(bool Success, string? ErrorMessage)> AddToCartAsync(int userId, AddToCartRequest request)
    {
        if (request.Quantity <= 0)
            return (false, "Количество должно быть больше 0");

        var product = await _context.Products.FindAsync(request.ProductId);
        if (product == null || !product.IsAvailable)
            return (false, "Товар не найден или недоступен");

        var existingItem = await _context.CartItems
            .FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == request.ProductId);

        int targetQuantity = request.Quantity + (existingItem?.Quantity ?? 0);

        if (targetQuantity > product.StockQuantity)
            return (false, $"Недостаточно остатков на складе. Доступно: {product.StockQuantity}");

        if (existingItem != null)
        {
            existingItem.Quantity = targetQuantity;
        }
        else
        {
            var cartItem = new CartItem
            {
                UserId = userId,
                ProductId = request.ProductId,
                Quantity = request.Quantity,
                CreatedAt = DateTime.UtcNow
            };
            _context.CartItems.Add(cartItem);
        }

        await _context.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? ErrorMessage)> UpdateQuantityAsync(int userId, int cartItemId, int newQuantity)
    {
        if (newQuantity <= 0)
            return (false, "Количество должно быть больше 0");

        var cartItem = await _context.CartItems
            .Include(c => c.Product)
            .FirstOrDefaultAsync(c => c.Id == cartItemId && c.UserId == userId);

        if (cartItem == null)
            return (false, "Позиция в корзине не найдена");

        if (newQuantity > cartItem.Product.StockQuantity)
            return (false, $"Недостаточно остатков на складе. Доступно: {cartItem.Product.StockQuantity}");

        cartItem.Quantity = newQuantity;
        await _context.SaveChangesAsync();

        return (true, null);
    }

    public async Task<bool> RemoveFromCartAsync(int userId, int cartItemId)
    {
        var cartItem = await _context.CartItems
            .FirstOrDefaultAsync(c => c.Id == cartItemId && c.UserId == userId);

        if (cartItem == null)
            return false;

        _context.CartItems.Remove(cartItem);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task ClearCartAsync(int userId)
    {
        var items = await _context.CartItems.Where(c => c.UserId == userId).ToListAsync();
        if (items.Any())
        {
            _context.CartItems.RemoveRange(items);
            await _context.SaveChangesAsync();
        }
    }
}