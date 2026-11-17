using Microsoft.EntityFrameworkCore;
using Pr4_Gordov.API.Data;
using Pr4_Gordov.API.DTO.Admin;
using Pr4_Gordov.API.DTO.Orders;
using Pr4_Gordov.API.DTO.Products;
using Pr4_Gordov.API.DTO.Reviews;
using Pr4_Gordov.API.Models;

namespace Pr4_Gordov.API.Services;

public class AdminService
{
    private readonly AppDbContext _context;

    public AdminService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<ProductResponse>> GetAllProductsAdminAsync()
    {
        var ratings = await _context.Reviews
            .GroupBy(r => r.ProductId)
            .Select(g => new { ProductId = g.Key, Avg = g.Average(r => r.Rating) })
            .ToDictionaryAsync(x => x.ProductId, x => x.Avg);

        var products = await _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .OrderByDescending(p => p.Id)
            .ToListAsync();

        return products.Select(p => new ProductResponse(
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
            ratings.TryGetValue(p.Id, out double avg) ? Math.Round(avg, 1) : 0.0
        )).ToList();
    }

    public async Task<ProductResponse> CreateProductAsync(UpsertProductRequest request)
    {
        var product = new Product
        {
            Title = request.Title,
            Description = request.Description,
            Price = request.Price,
            CategoryId = request.CategoryId,
            ImagePath = request.ImagePath,
            PackageQuantity = request.PackageQuantity,
            StockQuantity = request.StockQuantity,
            IsAvailable = request.IsAvailable,
            CreatedAt = DateTime.UtcNow
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        var category = await _context.Categories.FindAsync(product.CategoryId);

        return new ProductResponse(
            product.Id,
            product.Title,
            product.Description,
            product.Price,
            product.CategoryId,
            category?.Name ?? "",
            product.ImagePath,
            product.PackageQuantity,
            product.StockQuantity,
            product.IsAvailable,
            0.0
        );
    }

    public async Task<(bool Success, string? ErrorMessage)> UpdateProductAsync(int id, UpsertProductRequest request)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
            return (false, "Товар не найден");

        product.Title = request.Title;
        product.Description = request.Description;
        product.Price = request.Price;
        product.CategoryId = request.CategoryId;
        product.ImagePath = request.ImagePath;
        product.PackageQuantity = request.PackageQuantity;
        product.StockQuantity = request.StockQuantity;
        product.IsAvailable = request.IsAvailable;

        await _context.SaveChangesAsync();
        return (true, null);
    }

    public async Task<bool> ToggleProductAvailabilityAsync(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
            return false;

        product.IsAvailable = !product.IsAvailable;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<(bool Success, string? ErrorMessage)> DeleteProductAsync(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
            return (false, "Товар не найден");

        bool hasOrders = await _context.OrderItems.AnyAsync(oi => oi.ProductId == id);
        if (hasOrders)
        {
            product.IsAvailable = false;
            await _context.SaveChangesAsync();
            return (true, null);
        }

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();
        return (true, null);
    }

    public async Task<List<OrderResponse>> GetAllOrdersAsync()
    {
        return await _context.Orders
            .AsNoTracking()
            .OrderByDescending(o => o.OrderDate)
            .Include(o => o.Status)
            .Include(o => o.DeliveryType)
            .Include(o => o.PickupPoint)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .Select(o => new OrderResponse(
                o.Id,
                o.OrderDate,
                o.Status.Name,
                o.DeliveryType.Name,
                o.DeliveryAddress,
                o.PickupPoint != null ? o.PickupPoint.Name : null,
                o.PickupPoint != null ? o.PickupPoint.Address : null,
                o.TotalCost,
                o.OrderItems.Select(oi => new OrderItemResponse(
                    oi.Id,
                    oi.ProductId,
                    oi.Product.Title,
                    oi.Product.ImagePath,
                    oi.Quantity,
                    oi.UnitPrice,
                    oi.Quantity * oi.UnitPrice
                )).ToList()
            )).ToListAsync();
    }

    public async Task<(bool Success, string? ErrorMessage)> UpdateOrderStatusAsync(int orderId, int statusId)
    {
        var order = await _context.Orders.FindAsync(orderId);
        if (order == null)
            return (false, "Заказ не найден");

        var status = await _context.OrderStatuses.FindAsync(statusId);
        if (status == null)
            return (false, "Указанный статус не существует");

        order.StatusId = statusId;
        await _context.SaveChangesAsync();
        return (true, null);
    }

    public async Task<List<Category>> GetAllCategoriesAsync()
    {
        return await _context.Categories.AsNoTracking().ToListAsync();
    }

    public async Task<Category> CreateCategoryAsync(UpsertCategoryRequest request)
    {
        var category = new Category { Name = request.Name };
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();
        return category;
    }

    public async Task<bool> UpdateCategoryAsync(int id, UpsertCategoryRequest request)
    {
        var cat = await _context.Categories.FindAsync(id);
        if (cat == null) return false;
        cat.Name = request.Name;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<(bool Success, string? ErrorMessage)> DeleteCategoryAsync(int id)
    {
        var cat = await _context.Categories.FindAsync(id);
        if (cat == null) return (false, "Категория не найдена");
        bool hasProducts = await _context.Products.AnyAsync(p => p.CategoryId == id);
        if (hasProducts) return (false, "Нельзя удалить категорию, в которой есть товары");
        _context.Categories.Remove(cat);
        await _context.SaveChangesAsync();
        return (true, null);
    }

    public async Task<List<PickupPoint>> GetAllPickupPointsAsync()
    {
        return await _context.PickupPoints.AsNoTracking().ToListAsync();
    }

    public async Task<PickupPoint> CreatePickupPointAsync(UpsertPickupPointRequest request)
    {
        var point = new PickupPoint
        {
            Name = request.Name,
            Address = request.Address,
            WorkingHours = request.WorkingHours
        };
        _context.PickupPoints.Add(point);
        await _context.SaveChangesAsync();
        return point;
    }

    public async Task<bool> UpdatePickupPointAsync(int id, UpsertPickupPointRequest request)
    {
        var pt = await _context.PickupPoints.FindAsync(id);
        if (pt == null) return false;
        pt.Name = request.Name;
        pt.Address = request.Address;
        pt.WorkingHours = request.WorkingHours;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<(bool Success, string? ErrorMessage)> DeletePickupPointAsync(int id)
    {
        var pt = await _context.PickupPoints.FindAsync(id);
        if (pt == null) return (false, "ПВЗ не найден");
        bool hasOrders = await _context.Orders.AnyAsync(o => o.PickupPointId == id);
        if (hasOrders) return (false, "Нельзя удалить ПВЗ, к которому привязаны существующие заказы");
        _context.PickupPoints.Remove(pt);
        await _context.SaveChangesAsync();
        return (true, null);
    }

    public async Task<List<ReviewResponse>> GetAllReviewsAsync()
    {
        return await _context.Reviews
            .AsNoTracking()
            .Include(r => r.User)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new ReviewResponse(
                r.Id,
                r.ProductId,
                r.UserId,
                r.User.Login,
                r.Rating,
                r.Comment,
                r.CreatedAt
            ))
            .ToListAsync();
    }

    public async Task<bool> DeleteReviewAsync(int id)
    {
        var review = await _context.Reviews.FindAsync(id);
        if (review == null) return false;
        _context.Reviews.Remove(review);
        await _context.SaveChangesAsync();
        return true;
    }
}