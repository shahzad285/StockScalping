using System.Globalization;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using StockTrading.Common.DTOs;
using StockTrading.Common.Settings;
using StockTrading.IServices;

namespace StockTrading.Services;

public sealed class AlphaVantageFundamentalsService(
    HttpClient httpClient,
    IOptions<AlphaVantageSettings> options) : IAlphaVantageFundamentalsService
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
        var response = await httpClient.GetFromJsonAsync<Dictionary<string, string>>(requestUri, cancellationToken);

        if (response is null || response.Count == 0 || response.ContainsKey("Note") || response.ContainsKey("Error Message"))
        {
            return null;
        }

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
}
