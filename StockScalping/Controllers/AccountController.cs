using Microsoft.AspNetCore.Mvc;
using StockScalping.IServices;

namespace StockScalping.Controllers;

[ApiController]
[Route("[controller]")]
public class AccountController : ControllerBase
{
    private readonly IAngelOneService _angelOneService;
    private readonly ICacheService _cacheService;

    public AccountController(IAngelOneService angelOneService, ICacheService cacheService)
    {
        _angelOneService = angelOneService;
        _cacheService = cacheService;
    }

    [HttpGet("login")]
    public async Task<IActionResult> Login([FromQuery] string totp = null)
    {
        var isConnected = await _angelOneService.Login(totp);
        var message = isConnected ? "Login successful" : "Login failed";
        return Ok(new { message = message });
    }

    [HttpGet("mystocks")]
    public async Task<IActionResult> MyStocks()
    {
        try
        {
            var stocks = await _angelOneService.GetHoldingStocks();
            return Ok(new { stocks });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = "Failed to retrieve stocks", error = ex.Message });
        }
    }

    [HttpGet("debug/cache")]
    public IActionResult DebugCache()
    {
        const string refreshTokenKey = "AngelOne_RefreshToken";
        var refreshToken = _cacheService.GetValue(refreshTokenKey);
        var allKeys = _cacheService.GetAllKeys();

        return Ok(new
        {
            cachedKeys = allKeys,
            refreshTokenExists = refreshToken != null,
            refreshTokenLength = refreshToken?.Length ?? 0,
            refreshTokenPreview = refreshToken != null ? refreshToken.Substring(0, Math.Min(50, refreshToken.Length)) + "..." : "NOT FOUND"
        });
    }

    [HttpGet("debug/clearcache")]
    public IActionResult ClearCache()
    {
        _cacheService.ClearAll();
        return Ok(new { message = "Cache cleared successfully" });
    }

    [HttpGet("debug/tokens")]
    public IActionResult DebugTokens()
    {
        var cacheRefreshToken = _cacheService.GetValue("AngelOne_RefreshToken");
        
        return Ok(new
        {
            refreshTokenInCache = cacheRefreshToken != null,
            refreshTokenLength = cacheRefreshToken?.Length ?? 0,
            refreshTokenPreview = cacheRefreshToken != null ? cacheRefreshToken.Substring(0, Math.Min(30, cacheRefreshToken.Length)) + "..." : "NOT FOUND",
            message = "Check logs for JWT token status during API calls"
        });
    }
}
