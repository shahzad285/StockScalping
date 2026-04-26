using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.IO;
using StockScalping.IServices;

namespace StockScalping.Services;

public class AngelOneService : IAngelOneService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly ICacheService _cacheService;
    private readonly string _apiKey;
    private readonly string _secretKey;
    private readonly string _clientCode;
    private readonly string _password;
    private string _refreshToken;
    private string _jwtToken;
    private const string RefreshTokenCacheKey = "AngelOne_RefreshToken";

    public AngelOneService(HttpClient httpClient, IConfiguration config, ICacheService cacheService)
    {
        _httpClient = httpClient;
        _config = config;
        _cacheService = cacheService;
        _httpClient.BaseAddress = new Uri("https://apiconnect.angelone.in");
        
        // Get credentials
        _apiKey = _config["AngelOne:ApiKey"];
        _secretKey = _config["AngelOne:SecretKey"];
        _clientCode = _config["AngelOne:ClientCode"];
        _password = _config["AngelOne:Password"];
        
        // Load refresh token from cache
        _refreshToken = _cacheService.GetValue(RefreshTokenCacheKey);
    }

    private void SetDefaultHeaders(string token = null)
    {
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _httpClient.DefaultRequestHeaders.Add("X-UserType", "USER");
        _httpClient.DefaultRequestHeaders.Add("X-SourceID", "WEB");
        _httpClient.DefaultRequestHeaders.Add("X-ClientLocalIP", "127.0.0.1");
        _httpClient.DefaultRequestHeaders.Add("X-ClientPublicIP", "127.0.0.1");
        _httpClient.DefaultRequestHeaders.Add("X-MACAddress", "00:00:00:00:00:00");
        _httpClient.DefaultRequestHeaders.Add("X-PrivateKey", _apiKey);
        _httpClient.DefaultRequestHeaders.Add("X-SecretKey", _secretKey);
        
        if (!string.IsNullOrEmpty(token))
        {
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
        }
    }

    public async Task<bool> CheckConnection(string totp = null)
    {
        try
        {
            // Step 1: Get JWT token (either from refresh token or login)
            if (string.IsNullOrEmpty(_jwtToken))
            {
                bool tokenSuccess;
                
                // If we have refresh token, use it (M2M mode - no manual login needed)
                if (!string.IsNullOrEmpty(_refreshToken))
                {
                    tokenSuccess = await GenerateToken();
                }
                else
                {
                    // Otherwise, login manually with provided TOTP
                    if (string.IsNullOrEmpty(totp))
                    {
                        System.Console.WriteLine("TOTP required for first-time login. Pass ?totp=<code> in the URL");
                        return false;
                    }
                    tokenSuccess = await Login(totp);
                }
                
                if (!tokenSuccess)
                {
                    System.Console.WriteLine("Failed to obtain JWT token");
                    return false;
                }
            }

            // Step 2: Call getProfile with JWT token
            SetDefaultHeaders(_jwtToken);
            var response = await _httpClient.GetAsync("/rest/secure/angelbroking/user/v1/getProfile");
            
            var content = await response.Content.ReadAsStringAsync();
            System.Console.WriteLine($"Response Status: {response.StatusCode}");
            System.Console.WriteLine($"Response Body: {content}");
            
            if (!response.IsSuccessStatusCode)
                return false;
                
            var jsonDoc = JsonDocument.Parse(content);
            var root = jsonDoc.RootElement;
            
            // Check if API response indicates success
            if (root.TryGetProperty("status", out var statusElement))
            {
                return statusElement.GetBoolean();
            }
            
            return false;
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Connection Error: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> Login(string totp)
    {
        try
        {
            if (string.IsNullOrEmpty(totp))
            {
                System.Console.WriteLine("TOTP is required for login");
                return false;
            }
            
            SetDefaultHeaders();
            
            var loginRequest = new
            {
                clientcode = _clientCode,
                password = _password,
                totp = totp
            };

            var content = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(loginRequest),
                System.Text.Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(
                "/rest/auth/angelbroking/user/v1/loginByPassword",
                content);

            var responseContent = await response.Content.ReadAsStringAsync();
            System.Console.WriteLine($"Login Response Status: {response.StatusCode}");
            System.Console.WriteLine($"Login Response Body: {responseContent}");

            if (!response.IsSuccessStatusCode)
                return false;

            var jsonDoc = JsonDocument.Parse(responseContent);
            var root = jsonDoc.RootElement;

            if (root.TryGetProperty("status", out var statusElement) && statusElement.GetBoolean())
            {
                if (root.TryGetProperty("data", out var dataElement))
                {
                    if (dataElement.TryGetProperty("jwtToken", out var jwtElement))
                    {
                        _jwtToken = jwtElement.GetString();
                    }
                    
                    if (dataElement.TryGetProperty("refreshToken", out var refreshElement))
                    {
                        _refreshToken = refreshElement.GetString();
                        _cacheService.SetValue(RefreshTokenCacheKey, _refreshToken);
                        System.Console.WriteLine($"Refresh Token obtained and cached");
                        System.Console.WriteLine("✅ M2M mode enabled - future requests will use refresh token automatically");
                    }
                    
                    System.Console.WriteLine("Login successful, JWT token obtained");
                    return true;
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Login Error: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> GenerateToken()
    {
        try
        {
            SetDefaultHeaders();
            
            var tokenRequest = new
            {
                refreshToken = _refreshToken
            };

            var content = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(tokenRequest),
                System.Text.Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(
                "/rest/auth/angelbroking/jwt/v1/generateTokens",
                content);

            var responseContent = await response.Content.ReadAsStringAsync();
            System.Console.WriteLine($"Generate Token Response Status: {response.StatusCode}");
            System.Console.WriteLine($"Generate Token Response Body: {responseContent}");

            if (!response.IsSuccessStatusCode)
                return false;

            var jsonDoc = JsonDocument.Parse(responseContent);
            var root = jsonDoc.RootElement;

            if (root.TryGetProperty("status", out var statusElement) && statusElement.GetBoolean())
            {
                if (root.TryGetProperty("data", out var dataElement))
                {
                    if (dataElement.TryGetProperty("jwtToken", out var jwtElement))
                    {
                        _jwtToken = jwtElement.GetString();
                        System.Console.WriteLine("JWT token refreshed successfully (no TOTP needed)");
                        return true;
                    }
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Generate Token Error: {ex.Message}");
            return false;
        }
    }

    public async Task<decimal> GetCurrentPrice(string symbol)
    {
        // Placeholder implementation
        return 0m;
    }

    public async Task<bool> PlaceOrder(string symbol, int quantity, string orderType, decimal price)
    {
        // Placeholder implementation
        return false;
    }
}