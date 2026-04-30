using Microsoft.AspNetCore.Mvc;
using StockScalping.IServices;

namespace StockScalping.Controllers;

[ApiController]
[Route("[controller]")]
public class AccountController : ControllerBase
{
    private readonly IAngelOneService _angelOneService;

    public AccountController(IAngelOneService angelOneService)
    {
        _angelOneService = angelOneService;
    }

    [HttpGet("login")]
    public async Task<IActionResult> Login([FromQuery] string? totp = null)
    {
        var isConnected = await _angelOneService.Login(totp);
        var message = isConnected ? "Login successful" : "Login failed";
        return Ok(new { message = message });
    }

    [HttpGet("profile")]
    public async Task<IActionResult> Profile()
    {
        try
        {
            var profile = await _angelOneService.GetProfile();
            if (profile == null)
            {
                return Unauthorized(new { message = "Profile unavailable. Login first or refresh the session." });
            }

            return Ok(new { profile });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = "Failed to retrieve profile", error = ex.Message });
        }
    }
}
