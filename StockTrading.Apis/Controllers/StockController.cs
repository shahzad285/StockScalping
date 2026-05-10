using Microsoft.AspNetCore.Mvc;
using StockTrading.Common.DTOs;
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

    [HttpGet("chart")]
    public async Task<IActionResult> Chart(
        [FromQuery] string symbolToken,
        [FromQuery] StockExchange exchange = StockExchange.NSE,
        [FromQuery] StockChartRange range = StockChartRange.OneMonth)
    {
        if (string.IsNullOrWhiteSpace(symbolToken))
        {
            return BadRequest(new { message = "Symbol token is required." });
        }

        if (!Enum.IsDefined(exchange))
        {
            return BadRequest(new { message = "Exchange must be NSE or BSE." });
        }

        if (!Enum.IsDefined(range))
        {
            return BadRequest(new { message = "Unsupported chart range." });
        }

        try
        {
            var to = DateTime.Now;
            var (from, interval) = GetChartWindow(range, to);
            var candles = await _stockService.GetCandlesAsync(
                symbolToken,
                exchange,
                interval,
                from,
                to,
                HttpContext.RequestAborted);

            return Ok(new { candles });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = "Failed to retrieve stock chart", error = ex.Message });
        }
    }

    private static (DateTime From, StockChartInterval Interval) GetChartWindow(StockChartRange range, DateTime to)
    {
        return range switch
        {
            StockChartRange.OneDay => (to.AddDays(-1), StockChartInterval.FIVE_MINUTE),
            StockChartRange.OneWeek => (to.AddDays(-7), StockChartInterval.THIRTY_MINUTE),
            StockChartRange.OneMonth => (to.AddMonths(-1), StockChartInterval.ONE_DAY),
            StockChartRange.SixMonths => (to.AddMonths(-6), StockChartInterval.ONE_DAY),
            StockChartRange.OneYear => (to.AddYears(-1), StockChartInterval.ONE_DAY),
            _ => (to.AddMonths(-1), StockChartInterval.ONE_DAY)
        };
    }
}

public enum StockChartRange
{
    OneDay,
    OneWeek,
    OneMonth,
    SixMonths,
    OneYear
}
