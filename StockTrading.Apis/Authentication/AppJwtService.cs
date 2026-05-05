using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace StockTrading.Apis.Authentication;

public class AppJwtService : IAppJwtService
{
    private readonly IConfiguration _configuration;

    public AppJwtService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string CreateToken(string subject)
    {
        var now = DateTimeOffset.UtcNow;
        var expires = now.AddMinutes(GetExpiryMinutes());

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, subject),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, now.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(GetSecretKey())),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: GetIssuer(),
            audience: GetAudience(),
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expires.UtcDateTime,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string GetSecretKey()
    {
        var secretKey = _configuration["Jwt:SecretKey"];
        if (string.IsNullOrWhiteSpace(secretKey))
        {
            throw new InvalidOperationException("Jwt:SecretKey is required.");
        }

        return secretKey;
    }

    private string GetIssuer()
    {
        return _configuration["Jwt:Issuer"] ?? "StockTrading.Apis";
    }

    private string GetAudience()
    {
        return _configuration["Jwt:Audience"] ?? "StockTrading.Client";
    }

    private int GetExpiryMinutes()
    {
        return int.TryParse(_configuration["Jwt:ExpiryMinutes"], out var expiryMinutes) && expiryMinutes > 0
            ? expiryMinutes
            : 60;
    }
}
