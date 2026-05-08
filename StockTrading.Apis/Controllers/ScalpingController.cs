using Microsoft.AspNetCore.Mvc;
using StockTrading.IServices;

namespace StockTrading.Controllers;

[ApiController]
[Route("[controller]")]
public class ScalpingController : ControllerBase
{
    private readonly IScalpingQueryService _scalpingQueryService;

    public ScalpingController(IScalpingQueryService scalpingQueryService)
    {
        _scalpingQueryService = scalpingQueryService;
    }

    [HttpGet("status")]
    public async Task<IActionResult> Status()
    {
        var status = await _scalpingQueryService.GetStatusAsync(HttpContext.RequestAborted);
        return Ok(status);
    }

    [HttpGet("stocks")]
    public async Task<IActionResult> Stocks()
    {
        var stocks = await _scalpingQueryService.GetStocksAsync(HttpContext.RequestAborted);
        return Ok(new { stocks });
    }
}
