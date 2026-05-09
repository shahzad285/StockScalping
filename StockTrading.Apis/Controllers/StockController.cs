using Microsoft.AspNetCore.Mvc;
using StockTrading.IServices;

namespace StockTrading.Controllers;

[ApiController]
[Route("[controller]")]
public class StockController : ControllerBase
{
    private readonly IStockService _stockService;

    public StockController(IStockService stockService)
    {
        _stockService = stockService;
    }

    [HttpGet("holdings")]
    public async Task<IActionResult> Holdings()
    {
        try
        {
            var holdings = await _stockService.GetHoldingsAsync(HttpContext.RequestAborted);
            return Ok(holdings);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = "Failed to retrieve stocks", error = ex.Message });
        }
    }

    [HttpGet("prices")]
    public async Task<IActionResult> Prices()
    {
        try
        {
            var prices = await _stockService.GetConfiguredPricesAsync(HttpContext.RequestAborted);
            return Ok(new { prices });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = "Failed to retrieve stock prices", error = ex.Message });
        }
    }
}
