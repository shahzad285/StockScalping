using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StockTrading.Common.DTOs;
using StockTrading.Common.Settings;
using StockTrading.IServices;

namespace StockTrading.Services;

public sealed class FinnhubFundamentalsService(
    HttpClient httpClient,
    IOptions<FinnhubSettings> options,
    ILogger<FinnhubFundamentalsService> logger) : IFinnhubFundamentalsService
{
    private readonly FinnhubSettings _settings = options.Value;

    public async Task<FinnhubCompanyProfile?> GetCompanyProfileAsync(
        string symbol,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            logger.LogWarning("Finnhub:ApiKey is not configured. Skipping Finnhub fallback.");
            return null;
        }

        if (string.IsNullOrWhiteSpace(symbol))
        {
            return null;
        }

        var requestUri = $"stock/profile2?symbol={Uri.EscapeDataString(symbol.Trim())}&token={Uri.EscapeDataString(_settings.ApiKey)}";
        var response = await GetJsonOrDefaultAsync<Dictionary<string, object>>(
            requestUri,
            "company profile",
            "symbol",
            symbol,
            cancellationToken);

        if (response is null || response.Count == 0)
        {
            logger.LogWarning("Finnhub company profile response was empty for symbol {Symbol}.", symbol);
            return null;
        }

        if (response.TryGetValue("error", out var error))
        {
            logger.LogWarning("Finnhub company profile response included an error for symbol {Symbol}: {Error}", symbol, error);
            return null;
        }

        var profile = new FinnhubCompanyProfile
        {
            Ticker = GetValue(response, "ticker"),
            Name = GetValue(response, "name"),
            FinnhubIndustry = GetValue(response, "finnhubIndustry"),
            Exchange = GetValue(response, "exchange"),
            MarketCapitalization = GetDecimal(response, "marketCapitalization")
        };

        if (string.IsNullOrWhiteSpace(profile.Name) &&
            string.IsNullOrWhiteSpace(profile.FinnhubIndustry) &&
            profile.MarketCapitalization is null)
        {
            logger.LogWarning("Finnhub company profile did not include usable fundamentals for symbol {Symbol}.", symbol);
            return null;
        }

        logger.LogInformation(
            "Finnhub company profile returned name {Name}, industry {Industry}, market cap {MarketCap} for symbol {Symbol}.",
            profile.Name,
            profile.FinnhubIndustry,
            profile.MarketCapitalization,
            symbol);

        return profile;
    }

    public async Task<IReadOnlyList<FinnhubSymbolSearchMatch>> SearchSymbolsAsync(
        string keywords,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            logger.LogWarning("Finnhub:ApiKey is not configured. Skipping Finnhub symbol search.");
            return Array.Empty<FinnhubSymbolSearchMatch>();
        }

        if (string.IsNullOrWhiteSpace(keywords))
        {
            return Array.Empty<FinnhubSymbolSearchMatch>();
        }

        var requestUri = $"search?q={Uri.EscapeDataString(keywords.Trim())}&token={Uri.EscapeDataString(_settings.ApiKey)}";
        var response = await GetJsonOrDefaultAsync<Dictionary<string, object>>(
            requestUri,
            "symbol search",
            "keywords",
            keywords,
            cancellationToken);

        if (response is null)
        {
            logger.LogWarning("Finnhub symbol search response was null for keywords {Keywords}.", keywords);
            return Array.Empty<FinnhubSymbolSearchMatch>();
        }

        if (response.TryGetValue("error", out var error))
        {
            logger.LogWarning("Finnhub symbol search response included an error for keywords {Keywords}: {Error}", keywords, error);
            return Array.Empty<FinnhubSymbolSearchMatch>();
        }

        if (!response.TryGetValue("result", out var resultValue) ||
            resultValue is not System.Text.Json.JsonElement resultElement ||
            resultElement.ValueKind != System.Text.Json.JsonValueKind.Array)
        {
            logger.LogWarning("Finnhub symbol search returned no result array for keywords {Keywords}.", keywords);
            return Array.Empty<FinnhubSymbolSearchMatch>();
        }

        var matches = resultElement
            .EnumerateArray()
            .Select(match => new FinnhubSymbolSearchMatch
            {
                Symbol = GetJsonString(match, "symbol"),
                DisplaySymbol = GetJsonString(match, "displaySymbol"),
                Description = GetJsonString(match, "description"),
                Type = GetJsonString(match, "type")
            })
            .Where(match => !string.IsNullOrWhiteSpace(match.Symbol))
            .ToArray();

        logger.LogInformation(
            "Finnhub symbol search returned {Count} matches for keywords {Keywords}.",
            matches.Length,
            keywords);

        return matches;
    }

    private async Task<T?> GetJsonOrDefaultAsync<T>(
        string requestUri,
        string operation,
        string contextName,
        string contextValue,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await httpClient.GetAsync(requestUri, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.LogWarning(
                    "Finnhub {Operation} request failed for {ContextName} {ContextValue}. Status: {StatusCode}; Response: {ResponseBody}",
                    operation,
                    contextName,
                    contextValue,
                    (int)response.StatusCode,
                    Truncate(responseBody));

                return default;
            }

            return await response.Content.ReadFromJsonAsync<T>(cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(
                ex,
                "Finnhub {Operation} request failed for {ContextName} {ContextValue}.",
                operation,
                contextName,
                contextValue);
            return default;
        }
        catch (JsonException ex)
        {
            logger.LogWarning(
                ex,
                "Finnhub {Operation} response could not be parsed for {ContextName} {ContextValue}.",
                operation,
                contextName,
                contextValue);
            return default;
        }
    }

    private static string GetValue(IReadOnlyDictionary<string, object> values, string key)
    {
        return values.TryGetValue(key, out var value) ? Convert.ToString(value, CultureInfo.InvariantCulture) ?? "" : "";
    }

    private static decimal? GetDecimal(IReadOnlyDictionary<string, object> values, string key)
    {
        if (!values.TryGetValue(key, out var value))
        {
            return null;
        }

        return decimal.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }

    private static string GetJsonString(System.Text.Json.JsonElement element, string propertyName)
    {
        return element.ValueKind == System.Text.Json.JsonValueKind.Object &&
               element.TryGetProperty(propertyName, out var value) &&
               value.ValueKind == System.Text.Json.JsonValueKind.String
            ? value.GetString() ?? ""
            : "";
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
