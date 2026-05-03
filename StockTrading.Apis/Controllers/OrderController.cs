using Microsoft.AspNetCore.Mvc;
using StockTrading.IServices;

namespace StockTrading.Controllers;

[ApiController]
[Route("[controller]")]
public class OrderController : ControllerBase
{
    private readonly IAngelOneService _angelOneService;

    public OrderController(IAngelOneService angelOneService)
    {
        _angelOneService = angelOneService;
    }

    [HttpGet]
    public async Task<IActionResult> GetOrders()
    {
        try
        {
            var orders = await _angelOneService.GetOrders();
            return Ok(new { orders });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = "Failed to retrieve orders", error = ex.Message });
        }
    }
}
