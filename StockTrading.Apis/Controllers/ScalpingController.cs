using Microsoft.AspNetCore.Mvc;
using StockTrading.Models;

namespace StockTrading.Controllers;

[ApiController]
[Route("[controller]")]
public class ScalpingController : ControllerBase
{
    private readonly List<StockProfile> _stocks;

    public ScalpingController(IConfiguration config)
    {
        _stocks = config.GetSection("Trading:Stocks").Get<List<StockProfile>>() ?? new List<StockProfile>();
    }

    [HttpGet("status")]
    public IActionResult Status()
    {
        return Ok(new
        {
            isConfigured = _stocks.Count > 0,
            trackedStocks = _stocks.Count,
            tradableStocks = _stocks.Count(stock => stock.PurchaseRate.HasValue && stock.SalesRate.HasValue)
        });
    }

    [HttpGet("stocks")]
    public IActionResult Stocks()
    {
        return Ok(new { stocks = _stocks });
    }
}
