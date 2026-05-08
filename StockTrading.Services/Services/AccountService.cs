using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using StockTrading.Common.DTOs;
using StockTrading.IServices;
using StockTrading.Models;
using StockTrading.Repository.IRepository;

namespace StockTrading.Services;

public sealed class AccountService(
    IAngelOneService angelOneService,
    IAppJwtService jwtService,
    IApplicationUserRepository userRepository,
    IApplicationRoleRepository roleRepository,
    IApplicationOtpRepository otpRepository) : IAccountService
{
    private static readonly TimeSpan OtpLifetime = TimeSpan.FromMinutes(5);

    public async Task<AccountServiceResult<RegisterResponse>> RegisterAsync(
        RegisterRequest request,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        var validationErrors = ValidateMobileNumber(request.MobileNumber);
        if (validationErrors.Count > 0)
        {
            return AccountServiceResult<RegisterResponse>.BadRequest("Registration failed", validationErrors);
        }

        var hasUsers = await userRepository.AnyAsync(cancellationToken);
        if (hasUsers && !user.IsInRole(ApplicationRoleNames.Admin))
        {
            return AccountServiceResult<RegisterResponse>.Forbidden();
        }

        var existingUser = await userRepository.GetByMobileNumberAsync(request.MobileNumber, cancellationToken);
        if (existingUser != null)
        {
            return AccountServiceResult<RegisterResponse>.BadRequest("Registration failed", ["Mobile number already exists"]);
        }

        var roleName = hasUsers
            ? NormalizeRoleName(request.Role)
            : ApplicationRoleNames.Admin;

        var applicationUser = new ApplicationUser
        {
            MobileNumber = request.MobileNumber.Trim(),
            NormalizedMobileNumber = NormalizeMobileNumber(request.MobileNumber)
        };

        await roleRepository.EnsureRolesAsync([ApplicationRoleNames.Admin, ApplicationRoleNames.User], cancellationToken);
        await userRepository.AddAsync(applicationUser, cancellationToken);
        await roleRepository.AddUserToRoleAsync(applicationUser.Id, roleName, cancellationToken);

        return AccountServiceResult<RegisterResponse>.Ok(new RegisterResponse("Registration successful", roleName));
    }

    public async Task<AccountServiceResult<RequestLoginOtpResponse>> RequestLoginOtpAsync(
        RequestLoginOtpRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByMobileNumberAsync(request.MobileNumber, cancellationToken);
        if (user == null || !user.IsActive)
        {
            return AccountServiceResult<RequestLoginOtpResponse>.Unauthorized("Invalid mobile number");
        }

        var otp = GenerateOtp();
        var expiresAtUtc = DateTime.UtcNow.Add(OtpLifetime);
        await otpRepository.CreateAsync(user.Id, HashOtp(otp), expiresAtUtc, cancellationToken);

        return AccountServiceResult<RequestLoginOtpResponse>.Ok(new RequestLoginOtpResponse(
            "OTP generated",
            otp,
            expiresAtUtc));
    }

    public async Task<AccountServiceResult<LoginResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByMobileNumberAsync(request.MobileNumber, cancellationToken);
        if (user == null || !user.IsActive)
        {
            return AccountServiceResult<LoginResponse>.Unauthorized("Invalid mobile number or OTP");
        }

        var nowUtc = DateTime.UtcNow;
        var otpId = await otpRepository.GetValidOtpIdAsync(user.Id, HashOtp(request.Otp), nowUtc, cancellationToken);
        if (otpId == null)
        {
            return AccountServiceResult<LoginResponse>.Unauthorized("Invalid mobile number or OTP");
        }

        await otpRepository.MarkConsumedAsync(otpId.Value, nowUtc, cancellationToken);

        var roles = await roleRepository.GetUserRolesAsync(user.Id, cancellationToken);
        var token = jwtService.CreateToken(user, roles);

        return AccountServiceResult<LoginResponse>.Ok(new LoginResponse("Login successful", token, roles));
    }

    public async Task<AccountServiceResult<MeResponse>> MeAsync(
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return AccountServiceResult<MeResponse>.Unauthorized("User not found");
        }

        var applicationUser = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (applicationUser == null)
        {
            return AccountServiceResult<MeResponse>.Unauthorized("User not found");
        }

        var roles = await roleRepository.GetUserRolesAsync(applicationUser.Id, cancellationToken);
        return AccountServiceResult<MeResponse>.Ok(new MeResponse(applicationUser.Id, applicationUser.MobileNumber, roles));
    }

    public async Task<AccountServiceResult<object>> SmartApiLoginAsync(
        SmartApiLoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var isConnected = await angelOneService.Login(request.Totp);
        return isConnected
            ? AccountServiceResult<object>.Ok(new { message = "Broker login successful" })
            : AccountServiceResult<object>.Unauthorized("Broker login failed");
    }

    public async Task<AccountServiceResult<AccountProfile>> GetProfileAsync(
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var profile = await angelOneService.GetProfile();
            if (profile != null)
            {
                return AccountServiceResult<AccountProfile>.Ok(profile);
            }

            var message = user.IsInRole(ApplicationRoleNames.Admin)
                ? "Broker session expired. Please login to SmartAPI again using TOTP."
                : "Broker session expired. Please contact admin.";

            return AccountServiceResult<AccountProfile>.ServiceUnavailable("BROKER_AUTH_FAILED", message);
        }
        catch (Exception ex)
        {
            return AccountServiceResult<AccountProfile>.BadRequest("Failed to retrieve profile", [ex.Message]);
        }
    }

    private static List<string> ValidateMobileNumber(string mobileNumber)
    {
        if (string.IsNullOrWhiteSpace(mobileNumber))
        {
            return ["Mobile number is required"];
        }

        return [];
    }

    private static string NormalizeRoleName(string? role)
    {
        return string.Equals(role, ApplicationRoleNames.Admin, StringComparison.OrdinalIgnoreCase)
            ? ApplicationRoleNames.Admin
            : ApplicationRoleNames.User;
    }

    private static string NormalizeMobileNumber(string mobileNumber)
    {
        return mobileNumber.Trim();
    }

    private static string GenerateOtp()
    {
        return RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
    }

    private static string HashOtp(string otp)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(otp.Trim()));
        return Convert.ToHexString(bytes);
    }
}
