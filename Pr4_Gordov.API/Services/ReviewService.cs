using Microsoft.EntityFrameworkCore;
using Pr4_Gordov.API.Data;
using Pr4_Gordov.API.DTO.Reviews;
using Pr4_Gordov.API.Models;

namespace Pr4_Gordov.API.Services;

public class ReviewService
{
    private readonly AppDbContext _context;

    public ReviewService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<ReviewResponse>> GetReviewsByProductAsync(int productId)
    {
        return await _context.Reviews
            .AsNoTracking()
            .Where(r => r.ProductId == productId)
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

    public async Task<(bool Success, string? ErrorMessage)> AddReviewAsync(int userId, CreateReviewRequest request)
    {
        if (request.Rating < 1 || request.Rating > 5)
            return (false, "Оценка должна быть от 1 до 5");

        var product = await _context.Products.FindAsync(request.ProductId);
        if (product == null)
            return (false, "Товар не найден");

        bool alreadyReviewed = await _context.Reviews
            .AnyAsync(r => r.UserId == userId && r.ProductId == request.ProductId);

        if (alreadyReviewed)
            return (false, "Вы уже оставили отзыв на этот товар");

        var review = new Review
        {
            ProductId = request.ProductId,
            UserId = userId,
            Rating = request.Rating,
            Comment = request.Comment,
            CreatedAt = DateTime.UtcNow
        };

        _context.Reviews.Add(review);
        await _context.SaveChangesAsync();

        return (true, null);
    }

    public async Task<bool> DeleteReviewAsync(int reviewId)
    {
        var review = await _context.Reviews.FindAsync(reviewId);
        if (review == null)
            return false;

        _context.Reviews.Remove(review);
        await _context.SaveChangesAsync();
        return true;
    }
}