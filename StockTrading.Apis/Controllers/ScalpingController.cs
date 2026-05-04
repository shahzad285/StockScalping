using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockTrading.Data;

namespace StockTrading.Controllers;

[ApiController]
[Route("[controller]")]
public class ScalpingController : ControllerBase
{
    private readonly StockTradingDbContext _dbContext;

    public ScalpingController(StockTradingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("status")]
    public async Task<IActionResult> Status()
    {
        var stocks = await _dbContext.TrackedStocks.ToListAsync();

        return Ok(new
        {
            isConfigured = stocks.Count > 0,
            trackedStocks = stocks.Count,
            tradableStocks = stocks.Count(stock => stock.PurchaseRate.HasValue && stock.SalesRate.HasValue)
        });
    }

    [HttpGet("stocks")]
    public async Task<IActionResult> Stocks()
    {
        var stocks = await _dbContext.TrackedStocks.ToListAsync();
        return Ok(new { stocks });
    }
}
