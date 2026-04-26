using Microsoft.AspNetCore.Mvc;
using StockScalping.Services;

namespace StockScalping.Controllers;

[ApiController]
[Route("[controller]")]
public class ConnectionController : ControllerBase
{
    private readonly AngelOneService _angelOneService;

    public ConnectionController(AngelOneService angelOneService)
    {
        _angelOneService = angelOneService;
    }

    [HttpGet("check")]
    public async Task<IActionResult> CheckConnection()
    {
        var isConnected = await _angelOneService.CheckConnection();
        return Ok(new { connected = isConnected });
    }
}