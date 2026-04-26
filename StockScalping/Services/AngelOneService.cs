using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace StockScalping.Services;

public class AngelOneService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly string _apiKey;
    private readonly string _clientSecret;

    public AngelOneService(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient;
        _config = config;
        
        // Set base URL to angelone.in
        _httpClient.BaseAddress = new Uri("https://apiconnect.angelone.in");
        
        // Get credentials
        _apiKey = _config["AngelOne:ApiKey"];
        _clientSecret = _config["AngelOne:ClientSecret"];
        
      
        
        // Set headers
        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
        _httpClient.DefaultRequestHeaders.Add("X-ClientSecret", _clientSecret);
    }

    public async Task<bool> CheckConnection()
    {
        try
        {
            // Simple test endpoint to verify connection
            var response = await _httpClient.GetAsync("/rest/secure/angelbroking/user/v1/getProfile");
            
            var content = await response.Content.ReadAsStringAsync();
            System.Console.WriteLine($"Response Status: {response.StatusCode}");
            System.Console.WriteLine($"Response Body: {content}");
            
            // Angel One API returns 200 even on errors, check the JSON response body
            if (!response.IsSuccessStatusCode)
                return false;
                
            var jsonDoc = JsonDocument.Parse(content);
            var root = jsonDoc.RootElement;
            
            // Check if API response indicates success
            if (root.TryGetProperty("success", out var successElement))
            {
                return successElement.GetBoolean();
            }
            
            return false;
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Connection Error: {ex.Message}");
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