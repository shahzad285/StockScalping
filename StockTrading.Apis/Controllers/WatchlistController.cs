using Microsoft.AspNetCore.Mvc;
using StockTrading.Common.DTOs;
using StockTrading.IServices;

namespace StockTrading.Controllers;

[ApiController]
[Route("[controller]")]
public class WatchlistController : ControllerBase
{
    private readonly IWatchlistService _watchlistService;

    public WatchlistController(IWatchlistService watchlistService)
    {
        _watchlistService = watchlistService;
    }

    [HttpGet("stocks")]
    public async Task<IActionResult> Stocks()
    {
        var stocks = await _watchlistService.GetStocksAsync(HttpContext.RequestAborted);
        return Ok(new { stocks });
    }

    [HttpPost("stocks")]
    public async Task<IActionResult> SaveStock(WatchlistStock stock)
    {
        try
        {
            var savedStock = await _watchlistService.SaveStockAsync(stock, HttpContext.RequestAborted);
            return Ok(new { stock = savedStock });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("stocks/{symbol}")]
    public async Task<IActionResult> DeleteStock(string symbol)
    {
        await _watchlistService.DeleteStockAsync(symbol, HttpContext.RequestAborted);
        return NoContent();
    }

    [HttpDelete("stocks/by-id/{watchlistId:int}")]
    public async Task<IActionResult> DeleteStock(int watchlistId)
    {
        await _watchlistService.DeleteStockAsync(watchlistId, HttpContext.RequestAborted);
        return NoContent();
    }
}
