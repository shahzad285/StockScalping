using Microsoft.AspNetCore.Mvc;
using StockTrading.IServices;
using StockTrading.Models;

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

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string query, [FromQuery] StockExchange exchange = StockExchange.NSE)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return BadRequest(new { message = "Search query is required." });
        }

        if (!Enum.IsDefined(exchange))
        {
            return BadRequest(new { message = "Exchange must be NSE or BSE." });
        }

        try
        {
            var stocks = await _stockService.SearchStocksAsync(query, exchange, HttpContext.RequestAborted);
            return Ok(new { stocks });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = "Failed to search stocks", error = ex.Message });
        }
    }
}
