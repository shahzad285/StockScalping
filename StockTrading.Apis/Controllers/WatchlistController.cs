using Microsoft.AspNetCore.Mvc;
using StockTrading.Common.DTOs;
using StockTrading.IServices;

namespace StockTrading.Controllers;

[ApiController]
[Route("[controller]")]
public class WatchlistController : ControllerBase
{
    private readonly IWatchlistService _watchlistService;
    private readonly IStockService _stockService;

    public WatchlistController(IWatchlistService watchlistService, IStockService stockService)
    {
        _watchlistService = watchlistService;
        _stockService = stockService;
    }

    [HttpGet]
    public async Task<IActionResult> Watchlists()
    {
        var watchlists = await _watchlistService.GetWatchlistsAsync(HttpContext.RequestAborted);
        return Ok(new { watchlists });
    }

    [HttpPost]
    public async Task<IActionResult> CreateWatchlist(CreateWatchlistRequest request)
    {
        try
        {
            var watchlist = await _watchlistService.CreateWatchlistAsync(request.Name, HttpContext.RequestAborted);
            return Ok(new { watchlist });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteWatchlist(int id)
    {
        await _watchlistService.DeleteWatchlistAsync(id, HttpContext.RequestAborted);
        return NoContent();
    }

    [HttpGet("stocks")]
    public async Task<IActionResult> Stocks()
    {
        var stocks = await _watchlistService.GetStocksAsync(HttpContext.RequestAborted);
        return Ok(new { stocks });
    }

    [HttpGet("stocks/search")]
    public async Task<IActionResult> SearchStocks([FromQuery] string query, [FromQuery] StockExchange exchange = StockExchange.NSE)
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

    [HttpGet("{id:int}/stocks")]
    public async Task<IActionResult> WatchlistStocks(int id)
    {
        var stocks = await _watchlistService.GetStocksAsync(id, HttpContext.RequestAborted);
        return Ok(new { stocks });
    }

    [HttpPost("{id:int}/stocks")]
    public async Task<IActionResult> SaveWatchlistStock(int id, WatchlistStock stock)
    {
        try
        {
            var savedStock = await _watchlistService.SaveStockAsync(id, stock, HttpContext.RequestAborted);
            return Ok(new { stock = savedStock });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}/stocks/{watchlistItemId:int}")]
    public async Task<IActionResult> DeleteWatchlistStock(int id, int watchlistItemId)
    {
        await _watchlistService.DeleteStockAsync(id, watchlistItemId, HttpContext.RequestAborted);
        return NoContent();
    }
}

public sealed record CreateWatchlistRequest(string Name);
