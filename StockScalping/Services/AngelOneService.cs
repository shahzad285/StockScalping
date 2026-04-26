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
    private readonly string _clientLocalIP;
    private readonly string _clientPublicIP;
    private readonly string _macAddress;
    private string _refreshToken;
    private string _jwtToken;
    private const string RefreshTokenCacheKey = "AngelOne_RefreshToken";
    private const string JwtTokenCacheKey = "AngelOne_JwtToken";

    public AngelOneService(HttpClient httpClient, IConfiguration config, ICacheService cacheService)
    {
        _httpClient = httpClient;
        _config = config;
        _cacheService = cacheService;
        _httpClient.BaseAddress = new Uri("https://apiconnect.angelone.in");
        
        // Get credentials from appsettings
        _apiKey = _config["AngelOne:ApiKey"];
        _secretKey = _config["AngelOne:SecretKey"];
        _clientCode = _config["AngelOne:ClientCode"];
        _password = _config["AngelOne:Password"];
        _clientLocalIP = _config["AngelOne:ClientLocalIP"];
        _clientPublicIP = _config["AngelOne:ClientPublicIP"];
        _macAddress = _config["AngelOne:MACAddress"];
        
        // Load tokens from cache
        _refreshToken = _cacheService.GetValue(RefreshTokenCacheKey);
        _jwtToken = _cacheService.GetValue(JwtTokenCacheKey);
        
        System.Console.WriteLine($"AngelOne Service initialized with:");
        System.Console.WriteLine($"  LocalIP: {_clientLocalIP}");
        System.Console.WriteLine($"  PublicIP: {_clientPublicIP}");
        System.Console.WriteLine($"  MAC: {_macAddress}");
        System.Console.WriteLine($"  JWT Token from cache: {(_jwtToken != null ? "EXISTS" : "NULL")}");
        System.Console.WriteLine($"  Refresh Token from cache: {(_refreshToken != null ? "EXISTS" : "NULL")}");
    }

    private void SetDefaultHeaders(string token = null)
    {
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _httpClient.DefaultRequestHeaders.Add("X-UserType", "USER");
        _httpClient.DefaultRequestHeaders.Add("X-SourceID", "WEB");
        _httpClient.DefaultRequestHeaders.Add("X-ClientLocalIP", _clientLocalIP);
        _httpClient.DefaultRequestHeaders.Add("X-ClientPublicIP", _clientPublicIP);
        _httpClient.DefaultRequestHeaders.Add("X-MACAddress", _macAddress);
        _httpClient.DefaultRequestHeaders.Add("X-PrivateKey", _apiKey);
        _httpClient.DefaultRequestHeaders.Add("X-SecretKey", _secretKey);
        
        if (!string.IsNullOrEmpty(token))
        {
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
        }
    }

    public async Task<bool> Login(string totp = null)
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
                        System.Console.WriteLine("Login failed");
                        return false;
                    }
                    tokenSuccess = await LoginWithTotp(totp);
                }
                
                if (!tokenSuccess)
                {
                    System.Console.WriteLine("Failed to obtain JWT token");
                    System.Console.WriteLine("Login failed");
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
            {
                System.Console.WriteLine("Login failed");
                return false;
            }
                
            var jsonDoc = JsonDocument.Parse(content);
            var root = jsonDoc.RootElement;
            
            // Check if API response indicates success
            if (root.TryGetProperty("status", out var statusElement))
            {
                if (statusElement.GetBoolean())
                {
                    System.Console.WriteLine("Login successful");
                    return true;
                }
                else
                {
                    System.Console.WriteLine("Login failed");
                    return false;
                }
            }
            
            System.Console.WriteLine("Login failed");
            return false;
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Connection Error: {ex.Message}");
            System.Console.WriteLine("Login failed");
            return false;
        }
    }

    private async Task<bool> LoginWithTotp(string totp)
    {
        try
        {
            if (string.IsNullOrEmpty(totp))
            {
                System.Console.WriteLine("TOTP is required for login");
                System.Console.WriteLine("Login failed");
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
            {
                System.Console.WriteLine("Login failed");
                return false;
            }

            var jsonDoc = JsonDocument.Parse(responseContent);
            var root = jsonDoc.RootElement;

            if (root.TryGetProperty("status", out var statusElement) && statusElement.GetBoolean())
            {
                if (root.TryGetProperty("data", out var dataElement))
                {
                    if (dataElement.TryGetProperty("jwtToken", out var jwtElement))
                    {
                        _jwtToken = jwtElement.GetString();
                        System.Console.WriteLine($"JWT Token obtained (length: {_jwtToken?.Length ?? 0})");
                        _cacheService.SetValue(JwtTokenCacheKey, _jwtToken);
                        System.Console.WriteLine($"JWT Token cached with key: {JwtTokenCacheKey}");
                    }
                    
                    if (dataElement.TryGetProperty("refreshToken", out var refreshElement))
                    {
                        _refreshToken = refreshElement.GetString();
                        System.Console.WriteLine($"Refresh Token obtained (length: {_refreshToken?.Length ?? 0})");
                        System.Console.WriteLine($"Refresh Token preview: {_refreshToken?.Substring(0, Math.Min(50, _refreshToken.Length))}...");
                        
                        _cacheService.SetValue(RefreshTokenCacheKey, _refreshToken);
                        System.Console.WriteLine($"Refresh Token cached with key: {RefreshTokenCacheKey}");
                        
                        // Log token expiry information if available
                        if (dataElement.TryGetProperty("expiresIn", out var expiryElement))
                        {
                            System.Console.WriteLine($"Refresh Token TTL: {expiryElement.GetString()} seconds");
                        }
                        if (dataElement.TryGetProperty("refreshTokenExpiry", out var refreshExpiry))
                        {
                            System.Console.WriteLine($"Refresh Token Expiry: {refreshExpiry.GetString()}");
                        }
                        
                        System.Console.WriteLine("✅ M2M mode enabled - future requests will use refresh token automatically");
                    }
                    else
                    {
                        System.Console.WriteLine("⚠️ refreshToken property NOT FOUND in login response data");
                    }
                    
                    System.Console.WriteLine("Login successful, JWT token obtained");
                    return true;
                }
                else
                {
                    System.Console.WriteLine("⚠️ data property NOT FOUND in login response");
                }
            }
            else
            {
                System.Console.WriteLine("⚠️ status property is false or not found in login response");
            }

            return false;
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Login Error: {ex.Message}");
            System.Console.WriteLine("Login failed");
            return false;
        }
    }

    private async Task<bool> GenerateToken()
    {
        try
        {
            if (string.IsNullOrEmpty(_refreshToken))
            {
                System.Console.WriteLine("Refresh token is empty");
                return false;
            }

            System.Console.WriteLine($"Using refresh token: {_refreshToken.Substring(0, Math.Min(50, _refreshToken.Length))}... (length: {_refreshToken.Length})");

            // Set headers with the current JWT token in Authorization header
            SetDefaultHeaders(_jwtToken);
            
            var tokenRequest = new
            {
                refreshToken = _refreshToken
            };

            var requestBody = System.Text.Json.JsonSerializer.Serialize(tokenRequest);
            System.Console.WriteLine($"Request body: {requestBody}");
            System.Console.WriteLine($"Authorization header: Bearer {(_jwtToken != null ? _jwtToken.Substring(0, Math.Min(20, _jwtToken.Length)) : "NULL")}...");

            var content = new StringContent(
                requestBody,
                System.Text.Encoding.UTF8,
                "application/json");

            System.Console.WriteLine("Sending request to: /rest/auth/angelbroking/jwt/v1/generateTokens");

            var response = await _httpClient.PostAsync(
                "/rest/auth/angelbroking/jwt/v1/generateTokens",
                content);

            var responseContent = await response.Content.ReadAsStringAsync();
            System.Console.WriteLine($"Generate Token Response Status: {response.StatusCode}");
            System.Console.WriteLine($"Generate Token Response Body: {responseContent}");

            if (!response.IsSuccessStatusCode)
            {
                System.Console.WriteLine($"Generate Token failed with status code: {response.StatusCode}");
                return false;
            }

            var jsonDoc = JsonDocument.Parse(responseContent);
            var root = jsonDoc.RootElement;

            // Check for success property (Angel One API format)
            if (root.TryGetProperty("success", out var successElement) && successElement.GetBoolean())
            {
                if (root.TryGetProperty("data", out var dataElement))
                {
                    if (dataElement.TryGetProperty("jwtToken", out var jwtElement))
                    {
                        _jwtToken = jwtElement.GetString();
                        _cacheService.SetValue(JwtTokenCacheKey, _jwtToken);
                        System.Console.WriteLine("✅ JWT token refreshed successfully (no TOTP needed)");
                        System.Console.WriteLine($"JWT Token cached with key: {JwtTokenCacheKey}");
                        return true;
                    }
                    else
                    {
                        System.Console.WriteLine("jwtToken not found in response data");
                        return false;
                    }
                }
                else
                {
                    System.Console.WriteLine("data property not found in response");
                    return false;
                }
            }
            else
            {
                var message = root.TryGetProperty("message", out var msgElement) ? msgElement.GetString() : "Unknown error";
                var errorCode = root.TryGetProperty("errorCode", out var errElement) ? errElement.GetString() : "N/A";
                System.Console.WriteLine($"Token generation failed - Message: {message}, Error Code: {errorCode}");
                System.Console.WriteLine("Refresh token may be invalid or expired. Please re-login with TOTP.");
                return false;
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Generate Token Error: {ex.Message}");
            System.Console.WriteLine($"Stack Trace: {ex.StackTrace}");
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

    public async Task<List<dynamic>> GetHoldingStocks()
    {
        try
        {
            // Ensure JWT token is available
            if (string.IsNullOrEmpty(_jwtToken))
            {
                if (!string.IsNullOrEmpty(_refreshToken))
                {
                    await GenerateToken();
                }
                else
                {
                    return new List<dynamic>();
                }
            }

            SetDefaultHeaders(_jwtToken);
            
            var response = await _httpClient.GetAsync("/rest/secure/angelbroking/portfolio/v1/getHolding");
            
            if (!response.IsSuccessStatusCode)
            {
                System.Console.WriteLine($"Failed to fetch holdings: {response.StatusCode}");
                return new List<dynamic>();
            }

            var content = await response.Content.ReadAsStringAsync();
            var jsonDoc = JsonDocument.Parse(content);
            var root = jsonDoc.RootElement;

            var holdings = new List<dynamic>();

            if (root.TryGetProperty("data", out var dataElement) && dataElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in dataElement.EnumerateArray())
                {
                    var holding = new
                    {
                        Symbol = item.TryGetProperty("symbolname", out var sym) ? sym.GetString() : "",
                        Quantity = item.TryGetProperty("quantity", out var qty) ? int.Parse(qty.GetString() ?? "0") : 0,
                        AverageCost = item.TryGetProperty("avgprice", out var avg) ? decimal.Parse(avg.GetString() ?? "0") : 0m,
                        CurrentValue = item.TryGetProperty("ltp", out var ltp) ? decimal.Parse(ltp.GetString() ?? "0") : 0m
                    };
                    holdings.Add(holding);
                }
            }

            return holdings;
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Error fetching holdings: {ex.Message}");
            return new List<dynamic>();
        }
    }
}