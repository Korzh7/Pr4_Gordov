using Microsoft.EntityFrameworkCore;
using Pr4_Gordov.API.Data;
using Pr4_Gordov.API.DTO.Products;

namespace Pr4_Gordov.API.Services;

public class ProductService
{
    private readonly AppDbContext _context;

    public ProductService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<ProductResponse>> GetProductsAsync(
        string? search = null,
        int? categoryId = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        string? sortBy = null)
    {
        var query = _context.Products
            .AsNoTracking()
            .Where(p => p.IsAvailable && p.StockQuantity > 0);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(p => p.Title.Contains(search));
        }

        if (categoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == categoryId.Value);
        }

        if (minPrice.HasValue)
        {
            query = query.Where(p => p.Price >= minPrice.Value);
        }

        if (maxPrice.HasValue)
        {
            query = query.Where(p => p.Price <= maxPrice.Value);
        }

        query = sortBy?.ToLower() switch
        {
            "price_asc" => query.OrderBy(p => p.Price),
            "price_desc" => query.OrderByDescending(p => p.Price),
            _ => query.OrderByDescending(p => p.CreatedAt)
        };

        return await query.Select(p => new ProductResponse(
            p.Id,
            p.Title,
            p.Description,
            p.Price,
            p.CategoryId,
            p.Category.Name,
            p.ImagePath,
            p.PackageQuantity,
            p.StockQuantity,
            p.IsAvailable,
            p.Reviews.Any() ? Math.Round(p.Reviews.Average(r => r.Rating), 1) : 0.0
        )).ToListAsync();
    }

    public async Task<ProductResponse?> GetByIdAsync(int id)
    {
        return await _context.Products
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new ProductResponse(
                p.Id,
                p.Title,
                p.Description,
                p.Price,
                p.CategoryId,
                p.Category.Name,
                p.ImagePath,
                p.PackageQuantity,
                p.StockQuantity,
                p.IsAvailable,
                p.Reviews.Any() ? Math.Round(p.Reviews.Average(r => r.Rating), 1) : 0.0
            ))
            .FirstOrDefaultAsync();
    }
}