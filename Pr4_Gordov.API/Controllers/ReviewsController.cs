using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pr4_Gordov.API.DTO.Reviews;
using Pr4_Gordov.API.Services;

namespace Pr4_Gordov.API.Controllers;

[ApiController]
[Route("api/reviews")]
public class ReviewsController : ControllerBase
{
    private readonly ReviewService _reviewService;

    public ReviewsController(ReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    private int GetCurrentUserId()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.Parse(idClaim!);
    }

    [HttpGet("product/{productId}")]
    public async Task<IActionResult> GetByProduct(int productId)
    {
        var reviews = await _reviewService.GetReviewsByProductAsync(productId);
        return Ok(reviews);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> AddReview([FromBody] CreateReviewRequest request)
    {
        var (success, errorMessage) = await _reviewService.AddReviewAsync(GetCurrentUserId(), request);
        if (!success)
            return BadRequest(new { message = errorMessage });

        return Ok(new { message = "Отзыв успешно опубликован" });
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteReview(int id)
    {
        bool success = await _reviewService.DeleteReviewAsync(id);
        if (!success)
            return NotFound(new { message = "Отзыв не найден" });

        return Ok(new { message = "Отзыв удален" });
    }
}