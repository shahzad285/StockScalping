using Microsoft.AspNetCore.Mvc;
using StockScalping.IServices;

namespace StockScalping.Controllers;

[ApiController]
[Route("[controller]")]
public class StockController : ControllerBase
{
    private readonly IAngelOneService _angelOneService;

    public StockController(IAngelOneService angelOneService)
    {
        _angelOneService = angelOneService;
    }

    [HttpGet("holdings")]
    public async Task<IActionResult> Holdings()
    {
        try
        {
            var stocks = await _angelOneService.GetHoldingStocks();
            return Ok(new { stocks });
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
            var prices = await _angelOneService.GetConfiguredStockPrices();
            return Ok(new { prices });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = "Failed to retrieve stock prices", error = ex.Message });
        }
    }
}
