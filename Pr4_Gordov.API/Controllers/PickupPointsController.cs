using Microsoft.AspNetCore.Mvc;
using Pr4_Gordov.API.Services;

namespace Pr4_Gordov.API.Controllers;

[ApiController]
[Route("api/pickup-points")]
public class PickupPointsController : ControllerBase
{
    private readonly OrderService _orderService;

    public PickupPointsController(OrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var points = await _orderService.GetPickupPointsAsync();
        return Ok(points);
    }
}