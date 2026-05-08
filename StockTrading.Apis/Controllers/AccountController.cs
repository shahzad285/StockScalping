using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockTrading.Apis.Authentication;
using StockTrading.IServices;
using StockTrading.Models;

namespace StockTrading.Controllers;

[ApiController]
[Route("[controller]")]
public class AccountController : ControllerBase
{
    private readonly IAngelOneService _angelOneService;
    private readonly IAppJwtService _jwtService;
    private readonly UserManager<ApplicationUser> _userManager;

    public AccountController(
        IAngelOneService angelOneService,
        IAppJwtService jwtService,
        UserManager<ApplicationUser> userManager)
    {
        _angelOneService = angelOneService;
        _jwtService = jwtService;
        _userManager = userManager;
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var hasUsers = await _userManager.Users.AnyAsync();
        if (hasUsers && !User.IsInRole(ApplicationRoleNames.Admin))
        {
            return Forbid();
        }

        var roleName = hasUsers
            ? NormalizeRoleName(request.Role)
            : ApplicationRoleNames.Admin;

        var user = new ApplicationUser
        {
            UserName = request.UserName,
            Email = request.Email,
            EmailConfirmed = true
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            return BadRequest(new
            {
                message = "Registration failed",
                errors = createResult.Errors.Select(error => error.Description)
            });
        }

        var roleResult = await _userManager.AddToRoleAsync(user, roleName);
        if (!roleResult.Succeeded)
        {
            return BadRequest(new
            {
                message = "User created but role assignment failed",
                errors = roleResult.Errors.Select(error => error.Description)
            });
        }

        return Ok(new
        {
            message = "Registration successful",
            role = roleName
        });
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var user = await _userManager.FindByNameAsync(request.UserName);
        if (user == null || !user.IsActive || !await _userManager.CheckPasswordAsync(user, request.Password))
        {
            return Unauthorized(new { message = "Invalid username or password" });
        }

        var roles = await _userManager.GetRolesAsync(user);
        var token = _jwtService.CreateToken(user, roles.ToArray());

        return Ok(new
        {
            message = "Login successful",
            token,
            roles
        });
    }

    [Authorize(Roles = ApplicationRoleNames.Admin)]
    [HttpPost("smartapi/login")]
    public async Task<IActionResult> SmartApiLogin(SmartApiLoginRequest request)
    {
        var isConnected = await _angelOneService.Login(request.Totp);
        if (!isConnected)
        {
            return Unauthorized(new { message = "Broker login failed" });
        }

        return Ok(new { message = "Broker login successful" });
    }

    [HttpGet("profile")]
    public async Task<IActionResult> Profile()
    {
        try
        {
            var profile = await _angelOneService.GetProfile();
            if (profile == null)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                {
                    code = "BROKER_AUTH_FAILED",
                    message = User.IsInRole(ApplicationRoleNames.Admin)
                        ? "Broker session expired. Please login to SmartAPI again using TOTP."
                        : "Broker session expired. Please contact admin."
                });
            }

            return Ok(new { profile });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = "Failed to retrieve profile", error = ex.Message });
        }
    }

    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized(new { message = "User not found" });
        }

        var roles = await _userManager.GetRolesAsync(user);
        return Ok(new
        {
            user.Id,
            user.UserName,
            user.Email,
            roles
        });
    }

    private static string NormalizeRoleName(string? role)
    {
        return string.Equals(role, ApplicationRoleNames.Admin, StringComparison.OrdinalIgnoreCase)
            ? ApplicationRoleNames.Admin
            : ApplicationRoleNames.User;
    }

    public sealed record RegisterRequest(string UserName, string Email, string Password, string? Role);
    public sealed record LoginRequest(string UserName, string Password);
    public sealed record SmartApiLoginRequest(string? Totp);
}
