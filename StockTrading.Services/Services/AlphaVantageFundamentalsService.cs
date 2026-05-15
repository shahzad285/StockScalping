using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StockTrading.Common.DTOs;
using StockTrading.Common.Settings;
using StockTrading.IServices;

namespace StockTrading.Services;

public sealed class AlphaVantageFundamentalsService(
    HttpClient httpClient,
    IOptions<AlphaVantageSettings> options,
    ILogger<AlphaVantageFundamentalsService> logger) : IAlphaVantageFundamentalsService
{
    private readonly AlphaVantageSettings _settings = options.Value;

    public async Task<AlphaVantageCompanyOverview?> GetCompanyOverviewAsync(
        string symbol,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            throw new InvalidOperationException("AlphaVantage:ApiKey is required.");
        }

        if (string.IsNullOrWhiteSpace(symbol))
        {
            throw new ArgumentException("Symbol is required.", nameof(symbol));
        }

        var requestUri = $"query?function=OVERVIEW&symbol={Uri.EscapeDataString(symbol.Trim())}&apikey={Uri.EscapeDataString(_settings.ApiKey)}";
        var response = await GetJsonOrDefaultAsync<Dictionary<string, string>>(
            requestUri,
            "overview",
            "symbol",
            symbol,
            cancellationToken);

        if (response is null)
        {
            logger.LogWarning("Alpha Vantage overview response was null for symbol {Symbol}.", symbol);
            return null;
        }

        if (response.Count == 0)
        {
            logger.LogWarning("Alpha Vantage overview response was empty for symbol {Symbol}.", symbol);
            return null;
        }

        if (response.TryGetValue("Note", out var note))
        {
            logger.LogWarning("Alpha Vantage overview response included a note for symbol {Symbol}: {Note}", symbol, note);
            return null;
        }

        if (response.TryGetValue("Error Message", out var errorMessage))
        {
            logger.LogWarning("Alpha Vantage overview response included an error for symbol {Symbol}: {ErrorMessage}", symbol, errorMessage);
            return null;
        }

        logger.LogInformation(
            "Alpha Vantage overview response returned {FieldCount} fields for symbol {Symbol}.",
            response.Count,
            symbol);

        return new AlphaVantageCompanyOverview
        {
            Symbol = GetValue(response, "Symbol"),
            Name = GetValue(response, "Name"),
            Description = GetValue(response, "Description"),
            Sector = GetValue(response, "Sector"),
            Industry = GetValue(response, "Industry"),
            MarketCapitalization = GetDecimal(response, "MarketCapitalization"),
            PERatio = GetDecimal(response, "PERatio"),
            DividendYield = GetDecimal(response, "DividendYield"),
            DebtToEquity = GetDecimal(response, "DebtToEquityRatio")
        };
    }

    private static string GetValue(IReadOnlyDictionary<string, string> values, string key)
    {
        return values.TryGetValue(key, out var value) ? value : "";
    }

    private static decimal? GetDecimal(IReadOnlyDictionary<string, string> values, string key)
    {
        if (!values.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value) || value == "None")
        {
            return null;
        }

        return decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }

    public async Task<IReadOnlyList<AlphaVantageSymbolSearchMatch>> SearchSymbolsAsync(
        string keywords,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            throw new InvalidOperationException("AlphaVantage:ApiKey is required.");
        }

        if (string.IsNullOrWhiteSpace(keywords))
        {
            return Array.Empty<AlphaVantageSymbolSearchMatch>();
        }

        var requestUri = $"query?function=SYMBOL_SEARCH&keywords={Uri.EscapeDataString(keywords.Trim())}&apikey={Uri.EscapeDataString(_settings.ApiKey)}";
        var response = await GetJsonOrDefaultAsync<Dictionary<string, object>>(
            requestUri,
            "symbol search",
            "keywords",
            keywords,
            cancellationToken);

        if (response is null)
        {
            logger.LogWarning("Alpha Vantage symbol search response was null for keywords {Keywords}.", keywords);
            return Array.Empty<AlphaVantageSymbolSearchMatch>();
        }

        if (response.TryGetValue("Note", out var note))
        {
            logger.LogWarning("Alpha Vantage symbol search response included a note for keywords {Keywords}: {Note}", keywords, note);
            return Array.Empty<AlphaVantageSymbolSearchMatch>();
        }

        if (response.TryGetValue("Error Message", out var errorMessage))
        {
            logger.LogWarning("Alpha Vantage symbol search response included an error for keywords {Keywords}: {ErrorMessage}", keywords, errorMessage);
            return Array.Empty<AlphaVantageSymbolSearchMatch>();
        }

        if (!response.TryGetValue("bestMatches", out var bestMatchesValue) ||
            bestMatchesValue is not System.Text.Json.JsonElement bestMatchesElement ||
            bestMatchesElement.ValueKind != System.Text.Json.JsonValueKind.Array)
        {
            logger.LogWarning("Alpha Vantage symbol search returned no best matches for keywords {Keywords}.", keywords);
            return Array.Empty<AlphaVantageSymbolSearchMatch>();
        }

        var matches = bestMatchesElement
            .EnumerateArray()
            .Select(match => new AlphaVantageSymbolSearchMatch
            {
                Symbol = GetJsonString(match, "1. symbol"),
                Name = GetJsonString(match, "2. name"),
                Region = GetJsonString(match, "4. region"),
                MatchScore = GetJsonString(match, "9. matchScore")
            })
            .Where(match => !string.IsNullOrWhiteSpace(match.Symbol))
            .ToArray();

        logger.LogInformation(
            "Alpha Vantage symbol search returned {Count} matches for keywords {Keywords}.",
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
                    "Alpha Vantage {Operation} request failed for {ContextName} {ContextValue}. Status: {StatusCode}; Response: {ResponseBody}",
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
                "Alpha Vantage {Operation} request failed for {ContextName} {ContextValue}.",
                operation,
                contextName,
                contextValue);
            return default;
        }
        catch (JsonException ex)
        {
            logger.LogWarning(
                ex,
                "Alpha Vantage {Operation} response could not be parsed for {ContextName} {ContextValue}.",
                operation,
                contextName,
                contextValue);
            return default;
        }
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

    private static string GetJsonString(System.Text.Json.JsonElement element, string propertyName)
    {
        return element.ValueKind == System.Text.Json.JsonValueKind.Object &&
               element.TryGetProperty(propertyName, out var value) &&
               value.ValueKind == System.Text.Json.JsonValueKind.String
            ? value.GetString() ?? ""
            : "";
    }
}
