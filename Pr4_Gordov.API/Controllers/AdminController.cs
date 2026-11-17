using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pr4_Gordov.API.DTO.Admin;
using Pr4_Gordov.API.Services;

namespace Pr4_Gordov.API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly AdminService _adminService;

    public AdminController(AdminService adminService)
    {
        _adminService = adminService;
    }

    [HttpGet("products")]
    public async Task<IActionResult> GetAllProducts()
    {
        var products = await _adminService.GetAllProductsAdminAsync();
        return Ok(products);
    }

    [HttpPost("products")]
    public async Task<IActionResult> CreateProduct([FromBody] UpsertProductRequest request)
    {
        var product = await _adminService.CreateProductAsync(request);
        return Ok(product);
    }

    [HttpPut("products/{id}")]
    public async Task<IActionResult> UpdateProduct(int id, [FromBody] UpsertProductRequest request)
    {
        var (success, errorMessage) = await _adminService.UpdateProductAsync(id, request);
        if (!success)
            return BadRequest(new { message = errorMessage });

        return Ok(new { message = "Товар успешно обновлен" });
    }

    [HttpDelete("products/{id}")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        var (success, errorMessage) = await _adminService.DeleteProductAsync(id);
        if (!success)
            return BadRequest(new { message = errorMessage });

        return Ok(new { message = "Товар удален или архивирован" });
    }

    [HttpPut("products/{id}/toggle-availability")]
    public async Task<IActionResult> ToggleAvailability(int id)
    {
        bool success = await _adminService.ToggleProductAvailabilityAsync(id);
        if (!success)
            return NotFound(new { message = "Товар не найден" });

        return Ok(new { message = "Статус доступности товара изменен" });
    }

    [HttpGet("orders")]
    public async Task<IActionResult> GetAllOrders()
    {
        var orders = await _adminService.GetAllOrdersAsync();
        return Ok(orders);
    }

    [HttpPut("orders/{id}/status")]
    public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] UpdateOrderStatusRequest request)
    {
        var (success, errorMessage) = await _adminService.UpdateOrderStatusAsync(id, request.StatusId);
        if (!success)
            return BadRequest(new { message = errorMessage });

        return Ok(new { message = "Статус заказа успешно изменен" });
    }

    [HttpGet("categories")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCategories()
    {
        var categories = await _adminService.GetAllCategoriesAsync();
        return Ok(categories);
    }

    [HttpPost("categories")]
    public async Task<IActionResult> CreateCategory([FromBody] UpsertCategoryRequest request)
    {
        var category = await _adminService.CreateCategoryAsync(request);
        return Ok(category);
    }

    [HttpPut("categories/{id}")]
    public async Task<IActionResult> UpdateCategory(int id, [FromBody] UpsertCategoryRequest request)
    {
        bool ok = await _adminService.UpdateCategoryAsync(id, request);
        if (!ok) return NotFound(new { message = "Категория не найдена" });
        return Ok(new { message = "Категория обновлена" });
    }

    [HttpDelete("categories/{id}")]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var (success, error) = await _adminService.DeleteCategoryAsync(id);
        if (!success) return BadRequest(new { message = error });
        return Ok(new { message = "Категория удалена" });
    }

    [HttpGet("pickup-points")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPickupPoints()
    {
        var points = await _adminService.GetAllPickupPointsAsync();
        return Ok(points);
    }

    [HttpPost("pickup-points")]
    public async Task<IActionResult> CreatePickupPoint([FromBody] UpsertPickupPointRequest request)
    {
        var point = await _adminService.CreatePickupPointAsync(request);
        return Ok(point);
    }

    [HttpPut("pickup-points/{id}")]
    public async Task<IActionResult> UpdatePickupPoint(int id, [FromBody] UpsertPickupPointRequest request)
    {
        bool ok = await _adminService.UpdatePickupPointAsync(id, request);
        if (!ok) return NotFound(new { message = "ПВЗ не найден" });
        return Ok(new { message = "ПВЗ обновлен" });
    }

    [HttpDelete("pickup-points/{id}")]
    public async Task<IActionResult> DeletePickupPoint(int id)
    {
        var (success, error) = await _adminService.DeletePickupPointAsync(id);
        if (!success) return BadRequest(new { message = error });
        return Ok(new { message = "ПВЗ удален" });
    }

    [HttpGet("reviews")]
    public async Task<IActionResult> GetAllReviews()
    {
        var reviews = await _adminService.GetAllReviewsAsync();
        return Ok(reviews);
    }

    [HttpDelete("reviews/{id}")]
    public async Task<IActionResult> DeleteReview(int id)
    {
        bool ok = await _adminService.DeleteReviewAsync(id);
        if (!ok) return NotFound(new { message = "Отзыв не найден" });
        return Ok(new { message = "Отзыв удален" });
    }
}