using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockTrading.Apis.Authentication;
using StockTrading.IServices;

namespace StockTrading.Controllers;

[ApiController]
[Route("[controller]")]
public class AccountController : ControllerBase
{
    private readonly IAngelOneService _angelOneService;
    private readonly IAppJwtService _jwtService;

    public AccountController(IAngelOneService angelOneService, IAppJwtService jwtService)
    {
        _angelOneService = angelOneService;
        _jwtService = jwtService;
    }

    [AllowAnonymous]
    [HttpGet("login")]
    public async Task<IActionResult> Login([FromQuery] string? totp = null)
    {
        var isConnected = await _angelOneService.Login(totp);
        if (!isConnected)
        {
            return Unauthorized(new { message = "Login failed" });
        }

        var token = _jwtService.CreateToken("AngelOne");
        return Ok(new
        {
            message = "Login successful",
            token
        });
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
