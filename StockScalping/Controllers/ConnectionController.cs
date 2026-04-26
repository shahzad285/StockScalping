using Microsoft.AspNetCore.Mvc;
using StockScalping.IServices;

namespace StockScalping.Controllers;

[ApiController]
[Route("[controller]")]
public class ConnectionController : ControllerBase
{
    private readonly IAngelOneService _angelOneService;

    public ConnectionController(IAngelOneService angelOneService)
    {
        _angelOneService = angelOneService;
    }

    [HttpGet("check")]
    public async Task<IActionResult> CheckConnection([FromQuery] string totp = null)
    {
        var isConnected = await _angelOneService.CheckConnection(totp);
        return Ok(new { connected = isConnected });
    }
}