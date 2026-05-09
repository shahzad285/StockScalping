using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.IO;
using System.Globalization;
using Microsoft.Extensions.Configuration;
using StockTrading.Common.DTOs;
using StockTrading.IServices;
using StockTrading.Models;

namespace StockTrading.Services;

public class AngelOneService : IBrokerService
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
    private string? _refreshToken;
    private string? _jwtToken;
    private const string RefreshTokenCacheKey = "AngelOne_RefreshToken";
    private const string JwtTokenCacheKey = "AngelOne_JwtToken";

    public AngelOneService(HttpClient httpClient, IConfiguration config, ICacheService cacheService)
    {
        _httpClient = httpClient;
        _config = config;
        _cacheService = cacheService;
        _httpClient.BaseAddress = new Uri("https://apiconnect.angelone.in");

        // Get credentials from appsettings
        _apiKey = _config["AngelOne:ApiKey"] ?? "";
        _secretKey = _config["AngelOne:SecretKey"] ?? "";
        _clientCode = _config["AngelOne:ClientCode"] ?? "";
        _password = _config["AngelOne:Password"] ?? "";
        _clientLocalIP = _config["AngelOne:ClientLocalIP"] ?? "";
        _clientPublicIP = _config["AngelOne:ClientPublicIP"] ?? "";
        _macAddress = _config["AngelOne:MACAddress"] ?? "";

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

    private void SetDefaultHeaders(string? token = null)
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

    private async Task<bool> Login(string? totp = null)
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
                        if (!string.IsNullOrEmpty(_jwtToken))
                        {
                            _cacheService.SetValue(JwtTokenCacheKey, _jwtToken);
                        }
                        System.Console.WriteLine($"JWT Token cached with key: {JwtTokenCacheKey}");
                    }

                    if (dataElement.TryGetProperty("refreshToken", out var refreshElement))
                    {
                        _refreshToken = refreshElement.GetString();
                    if (!string.IsNullOrEmpty(_refreshToken))
                    {
                        _cacheService.SetValue(RefreshTokenCacheKey, _refreshToken);
                    }
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

            // Set headers with the current JWT token in Authorization header
            SetDefaultHeaders(_jwtToken);

            var tokenRequest = new
            {
                refreshToken = _refreshToken
            };

            var requestBody = System.Text.Json.JsonSerializer.Serialize(tokenRequest);
            System.Console.WriteLine($"Request body: {requestBody}");
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
                        if (!string.IsNullOrEmpty(_jwtToken))
                        {
                            _cacheService.SetValue(JwtTokenCacheKey, _jwtToken);
                        }
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

    private async Task<decimal> GetCurrentPrice(string symbol)
    {
        var prices = await GetCurrentPrices(new[]
        {
            new TrackedStock
            {
                Symbol = symbol,
                Exchange = "NSE"
            }
        });

        return prices.FirstOrDefault()?.LastTradedPrice ?? 0m;
    }

    private async Task<AccountProfile?> GetProfile()
    {
        try
        {
            if (!await EnsureJwtToken())
            {
                return null;
            }

            SetDefaultHeaders(_jwtToken);

            var response = await _httpClient.GetAsync("/rest/secure/angelbroking/user/v1/getProfile");
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                System.Console.WriteLine($"Failed to fetch profile: {response.StatusCode}");
                System.Console.WriteLine($"Profile Response Body: {content}");
                return null;
            }

            using var jsonDoc = JsonDocument.Parse(content);
            var root = jsonDoc.RootElement;

            if (!root.TryGetProperty("data", out var dataElement) ||
                dataElement.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            return new AccountProfile
            {
                ClientCode = GetJsonStringProperty(dataElement, "clientcode", "clientCode"),
                Name = GetJsonStringProperty(dataElement, "name", "clientname", "clientName"),
                Email = GetJsonStringProperty(dataElement, "email", "emailid", "emailId"),
                MobileNo = GetJsonStringProperty(dataElement, "mobileno", "mobileNo"),
                Broker = GetJsonStringProperty(dataElement, "broker", "brokerName"),
                Exchanges = GetJsonStringListProperty(dataElement, "exchanges", "exchange"),
                Products = GetJsonStringListProperty(dataElement, "products", "product")
            };
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Error fetching profile: {ex.Message}");
            return null;
        }
    }

    private async Task<List<StockPrice>> GetConfiguredStockPrices()
    {
        var stocks = _config.GetSection("Trading:Stocks").Get<List<TrackedStock>>() ?? new List<TrackedStock>();
        return await GetCurrentPrices(stocks);
    }

    private async Task<List<StockPrice>> GetCurrentPrices(IEnumerable<TrackedStock> stocks)
    {
        var stockList = stocks
            .Where(stock => !string.IsNullOrWhiteSpace(stock.Symbol) ||
                            !string.IsNullOrWhiteSpace(stock.TradingSymbol) ||
                            !string.IsNullOrWhiteSpace(stock.SymbolToken))
            .ToList();

        var prices = new List<StockPrice>();

        if (stockList.Count == 0)
        {
            return prices;
        }

        if (!await EnsureJwtToken())
        {
            return stockList.Select(stock => new StockPrice
            {
                Symbol = stock.Symbol,
                TradingSymbol = stock.TradingSymbol,
                Exchange = string.IsNullOrWhiteSpace(stock.Exchange) ? "NSE" : stock.Exchange,
                SymbolToken = stock.SymbolToken,
                IsFetched = false,
                Message = "JWT token is unavailable. Login first."
            }).ToList();
        }

        var instruments = new List<ScripInstrument>();

        foreach (var stock in stockList)
        {
            var instrument = await ResolveInstrument(stock);
            if (instrument == null)
            {
                prices.Add(new StockPrice
                {
                    Symbol = stock.Symbol,
                    TradingSymbol = stock.TradingSymbol,
                    Exchange = string.IsNullOrWhiteSpace(stock.Exchange) ? "NSE" : stock.Exchange,
                    SymbolToken = stock.SymbolToken,
                    IsFetched = false,
                    Message = "Unable to resolve symbol token."
                });
                continue;
            }

            instruments.Add(instrument);
        }

        foreach (var batch in instruments.Chunk(50))
        {
            var batchPrices = await FetchMarketQuoteBatch(batch);
            prices.AddRange(batchPrices);
        }

        return prices;
    }

    private async Task<bool> PlaceOrder(string symbol, int quantity, string orderType, decimal price)
    {
        // Placeholder implementation
        return false;
    }

    private sealed class ScripInstrument
    {
        public string Symbol { get; set; } = "";
        public string TradingSymbol { get; set; } = "";
        public string Exchange { get; set; } = "";
        public string SymbolToken { get; set; } = "";
    }

    private async Task<ScripInstrument?> ResolveInstrument(TrackedStock stock)
    {
        var exchange = string.IsNullOrWhiteSpace(stock.Exchange) ? "NSE" : stock.Exchange.Trim().ToUpperInvariant();
        var symbol = string.IsNullOrWhiteSpace(stock.Symbol) ? stock.TradingSymbol : stock.Symbol;
        var tradingSymbol = string.IsNullOrWhiteSpace(stock.TradingSymbol) ? symbol : stock.TradingSymbol;

        if (!string.IsNullOrWhiteSpace(stock.SymbolToken))
        {
            return new ScripInstrument
            {
                Symbol = symbol,
                TradingSymbol = tradingSymbol,
                Exchange = exchange,
                SymbolToken = stock.SymbolToken.Trim()
            };
        }

        var cacheKey = $"AngelOne_Scrip_{exchange}_{symbol}".ToUpperInvariant();
        var cachedScrip = _cacheService.GetValue(cacheKey);
        if (!string.IsNullOrWhiteSpace(cachedScrip))
        {
            var parts = cachedScrip.Split('|');
            if (parts.Length == 3)
            {
                return new ScripInstrument
                {
                    Symbol = symbol,
                    Exchange = parts[0],
                    TradingSymbol = parts[1],
                    SymbolToken = parts[2]
                };
            }
        }

        SetDefaultHeaders(_jwtToken);

        var searchRequest = new
        {
            exchange = exchange,
            searchscrip = symbol
        };

        var response = await _httpClient.PostAsJsonAsync(
            "/rest/secure/angelbroking/order/v1/searchScrip",
            searchRequest);
        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            System.Console.WriteLine($"Failed to resolve scrip {symbol}: {response.StatusCode}");
            System.Console.WriteLine($"Search Scrip Response Body: {content}");
            return null;
        }

        using var jsonDoc = JsonDocument.Parse(content);
        var root = jsonDoc.RootElement;

        if (!root.TryGetProperty("data", out var dataElement) ||
            dataElement.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var candidates = dataElement.EnumerateArray()
            .Select(item => new ScripInstrument
            {
                Symbol = symbol,
                Exchange = GetJsonStringProperty(item, "exchange"),
                TradingSymbol = GetJsonStringProperty(item, "tradingsymbol", "tradingSymbol", "symbol"),
                SymbolToken = GetJsonStringProperty(item, "symboltoken", "symbolToken")
            })
            .Where(item => !string.IsNullOrWhiteSpace(item.SymbolToken))
            .ToList();

        var selected = candidates.FirstOrDefault(item =>
                           string.Equals(item.TradingSymbol, symbol, StringComparison.OrdinalIgnoreCase)) ??
                       candidates.FirstOrDefault(item =>
                           string.Equals(item.TradingSymbol, $"{symbol}-EQ", StringComparison.OrdinalIgnoreCase)) ??
                       candidates.FirstOrDefault();

        if (selected != null)
        {
            _cacheService.SetValue(cacheKey, $"{selected.Exchange}|{selected.TradingSymbol}|{selected.SymbolToken}");
        }

        return selected;
    }

    private async Task<List<StockPrice>> FetchMarketQuoteBatch(IEnumerable<ScripInstrument> instruments)
    {
        var instrumentList = instruments.ToList();
        var prices = instrumentList.Select(instrument => new StockPrice
        {
            Symbol = instrument.Symbol,
            TradingSymbol = instrument.TradingSymbol,
            Exchange = instrument.Exchange,
            SymbolToken = instrument.SymbolToken,
            IsFetched = false,
            Message = "Quote not fetched."
        }).ToList();

        if (instrumentList.Count == 0)
        {
            return prices;
        }

        SetDefaultHeaders(_jwtToken);

        var exchangeTokens = instrumentList
            .GroupBy(instrument => instrument.Exchange)
            .ToDictionary(
                group => group.Key,
                group => group.Select(instrument => instrument.SymbolToken).Distinct().ToArray());

        var quoteRequest = new
        {
            mode = "LTP",
            exchangeTokens = exchangeTokens
        };

        var response = await _httpClient.PostAsJsonAsync(
            "/rest/secure/angelbroking/market/v1/quote/",
            quoteRequest);
        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            System.Console.WriteLine($"Failed to fetch market quote: {response.StatusCode}");
            System.Console.WriteLine($"Market Quote Response Body: {content}");
            foreach (var price in prices)
            {
                price.Message = $"Quote API failed with status {response.StatusCode}.";
            }

            return prices;
        }

        using var jsonDoc = JsonDocument.Parse(content);
        var root = jsonDoc.RootElement;

        if (!root.TryGetProperty("data", out var dataElement))
        {
            return prices;
        }

        if (dataElement.TryGetProperty("fetched", out var fetchedElement) &&
            fetchedElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in fetchedElement.EnumerateArray())
            {
                var exchange = GetJsonStringProperty(item, "exchange");
                var symbolToken = GetJsonStringProperty(item, "symbolToken", "symboltoken");
                var price = prices.FirstOrDefault(current =>
                    string.Equals(current.Exchange, exchange, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(current.SymbolToken, symbolToken, StringComparison.OrdinalIgnoreCase));

                if (price == null)
                {
                    continue;
                }

                price.TradingSymbol = GetJsonStringProperty(item, "tradingSymbol", "tradingsymbol", "symbol");
                price.LastTradedPrice = GetJsonDecimalProperty(item, "ltp", "lastTradedPrice");
                price.IsFetched = true;
                price.Message = "SUCCESS";
            }
        }

        if (dataElement.TryGetProperty("unfetched", out var unfetchedElement) &&
            unfetchedElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in unfetchedElement.EnumerateArray())
            {
                var exchange = GetJsonStringProperty(item, "exchange");
                var symbolToken = GetJsonStringProperty(item, "symbolToken", "symboltoken");
                var message = GetJsonStringProperty(item, "message", "errorMessage", "error");
                var price = prices.FirstOrDefault(current =>
                    string.Equals(current.Exchange, exchange, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(current.SymbolToken, symbolToken, StringComparison.OrdinalIgnoreCase));

                if (price != null)
                {
                    price.Message = string.IsNullOrWhiteSpace(message) ? "Quote was not returned by Angel One." : message;
                }
            }
        }

        return prices;
    }

    private static string GetJsonString(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? "",
            JsonValueKind.Number => element.GetRawText(),
            _ => ""
        };
    }

    private static int GetJsonInt(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Number)
        {
            if (element.TryGetInt32(out var intValue))
            {
                return intValue;
            }

            if (element.TryGetDecimal(out var decimalValue))
            {
                return (int)decimalValue;
            }
        }

        if (element.ValueKind == JsonValueKind.String &&
            decimal.TryParse(element.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedValue))
        {
            return (int)parsedValue;
        }

        return 0;
    }

    private static decimal GetJsonDecimal(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Number && element.TryGetDecimal(out var decimalValue))
        {
            return decimalValue;
        }

        if (element.ValueKind == JsonValueKind.String &&
            decimal.TryParse(element.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedValue))
        {
            return parsedValue;
        }

        return 0m;
    }

    private static decimal GetJsonDecimalProperty(JsonElement element, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (element.TryGetProperty(propertyName, out var value))
            {
                return GetJsonDecimal(value);
            }
        }

        return 0m;
    }

    private static string GetJsonStringProperty(JsonElement element, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (element.TryGetProperty(propertyName, out var value))
            {
                return GetJsonString(value);
            }
        }

        return "";
    }

    private static List<string> GetJsonStringListProperty(JsonElement element, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (!element.TryGetProperty(propertyName, out var value))
            {
                continue;
            }

            if (value.ValueKind == JsonValueKind.Array)
            {
                return value.EnumerateArray()
                    .Select(GetJsonString)
                    .Where(item => !string.IsNullOrWhiteSpace(item))
                    .ToList();
            }

            var stringValue = GetJsonString(value);
            if (!string.IsNullOrWhiteSpace(stringValue))
            {
                return new List<string> { stringValue };
            }
        }

        return new List<string>();
    }

    private static int GetJsonIntProperty(JsonElement element, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (element.TryGetProperty(propertyName, out var value))
            {
                return GetJsonInt(value);
            }
        }

        return 0;
    }

    private static string NormalizeOrderStatus(string status)
    {
        var normalizedStatus = status.Trim().ToLowerInvariant();

        return normalizedStatus switch
        {
            "complete" or "completed" or "executed" or "filled" => "Executed",
            "rejected" => "Rejected",
            "cancelled" or "canceled" => "Cancelled",
            "open" or "pending" or "trigger pending" or "validation pending" or "put order req received" => "Pending",
            _ => string.IsNullOrWhiteSpace(status) ? "Unknown" : "Other"
        };
    }

    private static IEnumerable<JsonElement> GetHoldingItems(JsonElement dataElement)
    {
        if (dataElement.ValueKind == JsonValueKind.Array)
        {
            return dataElement.EnumerateArray();
        }

        if (dataElement.ValueKind == JsonValueKind.Object &&
            dataElement.TryGetProperty("holdings", out var holdingsElement) &&
            holdingsElement.ValueKind == JsonValueKind.Array)
        {
            return holdingsElement.EnumerateArray();
        }

        return Enumerable.Empty<JsonElement>();
    }

    private async Task<bool> EnsureJwtToken()
    {
        if (!string.IsNullOrEmpty(_jwtToken))
        {
            return true;
        }

        if (string.IsNullOrEmpty(_refreshToken))
        {
            return false;
        }

        return await GenerateToken();
    }

    private async Task<HoldingsResponse> GetHoldingStocks()
    {
        try
        {
            // Ensure JWT token is available
            if (!await EnsureJwtToken())
            {
                return new HoldingsResponse();
            }

            SetDefaultHeaders(_jwtToken);

            var response = await _httpClient.GetAsync("/rest/secure/angelbroking/portfolio/v1/getHolding");

            if (!response.IsSuccessStatusCode)
            {
                System.Console.WriteLine($"Failed to fetch holdings: {response.StatusCode}");
                return new HoldingsResponse();
            }

            var content = await response.Content.ReadAsStringAsync();
            var jsonDoc = JsonDocument.Parse(content);
            var root = jsonDoc.RootElement;

            var holdings = new List<HoldingStock>();

            if (root.TryGetProperty("data", out var dataElement))
            {
                foreach (var item in GetHoldingItems(dataElement))
                {
                    var stockName = GetJsonStringProperty(item, "symbolname", "tradingsymbol", "symbol");
                    var tradingSymbol = GetJsonStringProperty(item, "tradingsymbol", "tradingSymbol", "symbol");
                    var purchasePrice = GetJsonDecimalProperty(item, "avgprice", "averageprice", "averagePrice");
                    var currentPrice = GetJsonDecimalProperty(item, "ltp", "currentprice", "currentPrice");

                    var holding = new HoldingStock
                    {
                        StockName = stockName,
                        TradingSymbol = tradingSymbol,
                        Exchange = GetJsonStringProperty(item, "exchange"),
                        SymbolToken = GetJsonStringProperty(item, "symboltoken", "symbolToken"),
                        PurchasePrice = purchasePrice,
                        TotalStocks = item.TryGetProperty("quantity", out var qty) ? GetJsonInt(qty) : 0,
                        CurrentPrice = currentPrice
                    };
                    holdings.Add(holding);
                }
            }

            var holdingPrices = await GetCurrentPrices(holdings.Select(holding => new TrackedStock
            {
                Symbol = string.IsNullOrWhiteSpace(holding.TradingSymbol) ? holding.StockName : holding.TradingSymbol,
                TradingSymbol = holding.TradingSymbol,
                Exchange = holding.Exchange,
                SymbolToken = holding.SymbolToken
            }));

            foreach (var holding in holdings)
            {
                var holdingPrice = holdingPrices.FirstOrDefault(price =>
                    !string.IsNullOrWhiteSpace(holding.SymbolToken) &&
                    string.Equals(price.SymbolToken, holding.SymbolToken, StringComparison.OrdinalIgnoreCase));

                if (holdingPrice == null && !string.IsNullOrWhiteSpace(holding.TradingSymbol))
                {
                    holdingPrice = holdingPrices.FirstOrDefault(price =>
                        string.Equals(price.TradingSymbol, holding.TradingSymbol, StringComparison.OrdinalIgnoreCase));
                }

                if (holdingPrice?.IsFetched == true)
                {
                    holding.CurrentPrice = holdingPrice.LastTradedPrice;
                }
            }

            return new HoldingsResponse
            {
                Stocks = holdings
            };
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Error fetching holdings: {ex.Message}");
            return new HoldingsResponse();
        }
    }

    private async Task<List<OrderDetails>> GetOrders()
    {
        try
        {
            if (!await EnsureJwtToken())
            {
                return new List<OrderDetails>();
            }

            SetDefaultHeaders(_jwtToken);

            var response = await _httpClient.GetAsync("/rest/secure/angelbroking/order/v1/getOrderBook");
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                System.Console.WriteLine($"Failed to fetch orders: {response.StatusCode}");
                System.Console.WriteLine($"Order Book Response Body: {content}");
                return new List<OrderDetails>();
            }

            var jsonDoc = JsonDocument.Parse(content);
            var root = jsonDoc.RootElement;
            var orders = new List<OrderDetails>();

            if (!root.TryGetProperty("data", out var dataElement) ||
                dataElement.ValueKind != JsonValueKind.Array)
            {
                return orders;
            }

            foreach (var item in dataElement.EnumerateArray())
            {
                var status = GetJsonStringProperty(item, "orderstatus", "status");

                orders.Add(new OrderDetails
                {
                    OrderId = GetJsonStringProperty(item, "orderid"),
                    TradingSymbol = GetJsonStringProperty(item, "tradingsymbol", "symbol"),
                    Exchange = GetJsonStringProperty(item, "exchange"),
                    TransactionType = GetJsonStringProperty(item, "transactiontype"),
                    OrderType = GetJsonStringProperty(item, "ordertype"),
                    ProductType = GetJsonStringProperty(item, "producttype"),
                    Duration = GetJsonStringProperty(item, "duration"),
                    Status = status,
                    StatusCategory = NormalizeOrderStatus(status),
                    RejectionReason = GetJsonStringProperty(item, "text", "rejreason", "rejectionreason"),
                    Quantity = GetJsonIntProperty(item, "quantity"),
                    FilledShares = GetJsonIntProperty(item, "filledshares", "filledquantity", "fillsize"),
                    UnfilledShares = GetJsonIntProperty(item, "unfilledshares", "unfilledquantity"),
                    CancelledShares = GetJsonIntProperty(item, "cancelsize", "cancelledshares", "cancelledquantity"),
                    Price = GetJsonDecimalProperty(item, "price"),
                    TriggerPrice = GetJsonDecimalProperty(item, "triggerprice"),
                    AveragePrice = GetJsonDecimalProperty(item, "averageprice", "avgprice"),
                    UpdateTime = GetJsonStringProperty(item, "updatetime"),
                    ExchangeTime = GetJsonStringProperty(item, "exchtime", "exchorderupdatetime"),
                    ParentOrderId = GetJsonStringProperty(item, "parentorderid")
                });
            }

            return orders;
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Error fetching orders: {ex.Message}");
            return new List<OrderDetails>();
        }
    }

    public Task<bool> LoginAsync(string? otp = null)
    {
        return Login(otp);
    }

    public Task<AccountProfile?> GetProfileAsync()
    {
        return GetProfile();
    }

    public Task<HoldingsResponse> GetHoldingsAsync()
    {
        return GetHoldingStocks();
    }

    public Task<List<StockPrice>> GetPricesAsync(IEnumerable<TrackedStock> stocks)
    {
        return GetCurrentPrices(stocks);
    }

    public Task<List<OrderDetails>> GetOrdersAsync()
    {
        return GetOrders();
    }

    public async Task<PlaceOrderResult> PlaceOrderAsync(PlaceOrderRequest request)
    {
        var isPlaced = await PlaceOrder(
            request.Symbol,
            request.Quantity,
            request.TransactionType,
            request.Price);

        return isPlaced
            ? new PlaceOrderResult(true, Message: "Order placed.")
            : new PlaceOrderResult(false, Message: "Order placement is not implemented for Angel One yet.");
    }

    public Task<CancelOrderResult> CancelOrderAsync(string brokerOrderId)
    {
        return Task.FromResult(new CancelOrderResult(
            false,
            brokerOrderId,
            "Order cancellation is not implemented for Angel One yet."));
    }
}
