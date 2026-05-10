using Microsoft.AspNetCore.Mvc;
using StockTrading.Common.DTOs;
using StockTrading.IServices;
using StockTrading.Models;

namespace StockTrading.Controllers;

[ApiController]
[Route("[controller]")]
public class TradePlanController : ControllerBase
{
    private readonly ITradePlanService _tradePlanService;
    private readonly IStockService _stockService;

    public TradePlanController(ITradePlanService tradePlanService, IStockService stockService)
    {
        _tradePlanService = tradePlanService;
        _stockService = stockService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var tradePlans = await _tradePlanService.GetAllAsync(HttpContext.RequestAborted);
        return Ok(new { tradePlans });
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

    [HttpPost]
    public async Task<IActionResult> Save(TradePlan tradePlan)
    {
        try
        {
            var savedTradePlan = await _tradePlanService.SaveAsync(tradePlan, HttpContext.RequestAborted);
            return Ok(new { tradePlan = savedTradePlan });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _tradePlanService.DeleteAsync(id, HttpContext.RequestAborted);
        return NoContent();
    }
}
