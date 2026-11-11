using Microsoft.AspNetCore.Mvc;
using Pr4_Gordov.API.Services;

namespace Pr4_Gordov.API.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly ProductService _productService;

    public ProductsController(ProductService productService)
    {
        _productService = productService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] int? categoryId,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        [FromQuery] string? sortBy)
    {
        var products = await _productService.GetProductsAsync(search, categoryId, minPrice, maxPrice, sortBy);
        return Ok(products);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var product = await _productService.GetByIdAsync(id);
        if (product == null)
            return NotFound("Товар не найден или недоступен для покупки");

        return Ok(product);
    }
}