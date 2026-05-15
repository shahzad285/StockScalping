using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StockTrading.Common.DTOs;
using StockTrading.Common.Settings;
using StockTrading.IServices;

namespace StockTrading.Services;

public sealed class TapetideFundamentalsService(
    HttpClient httpClient,
    IOptions<TapetideSettings> options,
    ILogger<TapetideFundamentalsService> logger) : ITapetideFundamentalsService
{
    private readonly TapetideSettings _settings = options.Value;

    public async Task<TapetideCompanyProfile?> GetCompanyProfileAsync(
        string symbol,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiToken))
        {
            logger.LogWarning("Tapetide:ApiToken is not configured. Skipping Tapetide fundamentals.");
            return null;
        }

        if (string.IsNullOrWhiteSpace(symbol))
        {
            return null;
        }

        try
        {
            var sessionId = await InitializeSessionAsync(cancellationToken);
            using var response = await SendMcpRequestAsync(
                new
                {
                    jsonrpc = "2.0",
                    id = Guid.NewGuid().ToString("N"),
                    method = "tools/call",
                    @params = new
                    {
                        name = "get_company_profile",
                        arguments = new
                        {
                            symbol = symbol.Trim().ToUpperInvariant()
                        }
                    }
                },
                sessionId,
                cancellationToken);

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Tapetide company profile request failed for symbol {Symbol}. Status: {StatusCode}; Response: {ResponseBody}",
                    symbol,
                    (int)response.StatusCode,
                    Truncate(responseBody));
                return null;
            }

            using var document = JsonDocument.Parse(GetJsonPayload(responseBody));
            if (document.RootElement.TryGetProperty("error", out var error))
            {
                logger.LogWarning(
                    "Tapetide company profile response included an error for symbol {Symbol}: {Error}",
                    symbol,
                    Truncate(error.ToString()));
                return null;
            }

            var profile = TryReadProfile(document.RootElement, symbol);
            if (profile is null ||
                (string.IsNullOrWhiteSpace(profile.Name) &&
                 string.IsNullOrWhiteSpace(profile.Sector) &&
                 string.IsNullOrWhiteSpace(profile.Industry) &&
                 profile.MarketCapitalization is null &&
                 profile.PERatio is null))
            {
                logger.LogWarning("Tapetide company profile did not include usable fundamentals for symbol {Symbol}.", symbol);
                return null;
            }

            logger.LogInformation(
                "Tapetide company profile returned name {Name}, sector {Sector}, industry {Industry}, market cap {MarketCap}, PE {PERatio} for symbol {Symbol}.",
                profile.Name,
                profile.Sector,
                profile.Industry,
                profile.MarketCapitalization,
                profile.PERatio,
                symbol);

            return profile;
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Tapetide company profile request failed for symbol {Symbol}.", symbol);
            return null;
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Tapetide company profile response could not be parsed for symbol {Symbol}.", symbol);
            return null;
        }
    }

    private async Task<string?> InitializeSessionAsync(CancellationToken cancellationToken)
    {
        using var response = await SendMcpRequestAsync(
            new
            {
                jsonrpc = "2.0",
                id = Guid.NewGuid().ToString("N"),
                method = "initialize",
                @params = new
                {
                    protocolVersion = "2025-03-26",
                    capabilities = new { },
                    clientInfo = new
                    {
                        name = "StockTrading",
                        version = "1.0"
                    }
                }
            },
            sessionId: null,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogWarning(
                "Tapetide initialize request failed. Status: {StatusCode}; Response: {ResponseBody}",
                (int)response.StatusCode,
                Truncate(responseBody));
            return null;
        }

        var sessionId = response.Headers.TryGetValues("Mcp-Session-Id", out var sessionValues)
            ? sessionValues.FirstOrDefault()
            : null;

        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            using var initializedResponse = await SendMcpRequestAsync(
                new
                {
                    jsonrpc = "2.0",
                    method = "notifications/initialized"
                },
                sessionId,
                cancellationToken);
        }

        return sessionId;
    }

    private async Task<HttpResponseMessage> SendMcpRequestAsync(
        object payload,
        string? sessionId,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "")
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            request.Headers.TryAddWithoutValidation("Mcp-Session-Id", sessionId);
        }

        return await httpClient.SendAsync(request, cancellationToken);
    }

    private static TapetideCompanyProfile? TryReadProfile(JsonElement root, string requestedSymbol)
    {
        var data = GetToolData(root);
        if (data.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
        {
            return null;
        }

        return new TapetideCompanyProfile
        {
            Symbol = GetString(data, "symbol", "ticker", "nse_symbol", "trading_symbol") ?? requestedSymbol,
            Name = GetString(data, "name", "company_name", "companyName") ?? "",
            Sector = GetString(data, "sector") ?? "",
            Industry = GetString(data, "industry", "sub_industry", "subIndustry") ?? "",
            Description = GetString(data, "description", "about", "business_summary") ?? "",
            MarketCapitalization = GetDecimal(data, "market_cap", "marketCap", "marketCapitalization", "Market Cap"),
            PERatio = GetDecimal(data, "pe", "pe_ratio", "peRatio", "PERatio", "Stock P/E"),
            DividendYield = GetDecimal(data, "dividend_yield", "dividendYield", "Dividend Yield"),
            DebtToEquity = GetDecimal(data, "debt_to_equity", "debtToEquity", "Debt to Equity")
        };
    }

    private static JsonElement GetToolData(JsonElement root)
    {
        if (!root.TryGetProperty("result", out var result))
        {
            return root;
        }

        if (result.TryGetProperty("structuredContent", out var structuredContent))
        {
            return structuredContent;
        }

        if (!result.TryGetProperty("content", out var content) ||
            content.ValueKind != JsonValueKind.Array)
        {
            return result;
        }

        foreach (var item in content.EnumerateArray())
        {
            if (item.TryGetProperty("text", out var textElement) &&
                textElement.ValueKind == JsonValueKind.String)
            {
                var text = textElement.GetString();
                if (!string.IsNullOrWhiteSpace(text) &&
                    TryParseJson(text, out var parsed))
                {
                    using (parsed)
                    {
                        return parsed.RootElement.Clone();
                    }
                }
            }
        }

        return result;
    }

    private static string? GetString(JsonElement element, params string[] propertyNames)
    {
        var value = FindProperty(element, propertyNames);
        return value is null ? null : ConvertElementToString(value.Value);
    }

    private static decimal? GetDecimal(JsonElement element, params string[] propertyNames)
    {
        var value = FindProperty(element, propertyNames);
        if (value is null)
        {
            return null;
        }

        if (value.Value.ValueKind == JsonValueKind.Number &&
            value.Value.TryGetDecimal(out var number))
        {
            return number;
        }

        var text = ConvertElementToString(value.Value);
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        text = text
            .Replace(",", "", StringComparison.Ordinal)
            .Replace("\u20b9", "", StringComparison.Ordinal)
            .Replace("%", "", StringComparison.Ordinal)
            .Replace("Cr.", "", StringComparison.OrdinalIgnoreCase)
            .Replace("Cr", "", StringComparison.OrdinalIgnoreCase)
            .Trim();

        return decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }

    private static JsonElement? FindProperty(JsonElement element, params string[] propertyNames)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (propertyNames.Any(name => property.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                {
                    return property.Value;
                }
            }

            foreach (var property in element.EnumerateObject())
            {
                var value = FindProperty(property.Value, propertyNames);
                if (value is not null)
                {
                    return value;
                }
            }
        }

        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                var value = FindProperty(item, propertyNames);
                if (value is not null)
                {
                    return value;
                }
            }
        }

        return null;
    }

    private static string ConvertElementToString(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? "",
            JsonValueKind.Number => element.ToString(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => ""
        };
    }

    private static bool TryParseJson(string text, out JsonDocument document)
    {
        try
        {
            document = JsonDocument.Parse(text);
            return true;
        }
        catch (JsonException)
        {
            document = null!;
            return false;
        }
    }

    private static string GetJsonPayload(string responseBody)
    {
        responseBody = responseBody.Trim();
        if (responseBody.StartsWith("{", StringComparison.Ordinal) ||
            responseBody.StartsWith("[", StringComparison.Ordinal))
        {
            return responseBody;
        }

        foreach (var line in responseBody.Split('\n'))
        {
            var trimmedLine = line.Trim();
            if (!trimmedLine.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var payload = trimmedLine["data:".Length..].Trim();
            if (!string.IsNullOrWhiteSpace(payload) &&
                !payload.Equals("[DONE]", StringComparison.OrdinalIgnoreCase))
            {
                return payload;
            }
        }

        return responseBody;
    }

    private static string Truncate(string value)
    {
        const int maxLength = 500;
        if (string.IsNullOrWhiteSpace(value))
        {
            return "";
        }

        value = value.Trim();
        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
