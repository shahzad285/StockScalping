using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StockTrading.Common.DTOs;
using StockTrading.Common.Settings;
using StockTrading.IServices;

namespace StockTrading.Services;

public sealed class NseIndiaService(
    HttpClient httpClient,
    IOptions<NseIndiaSettings> options,
    ILogger<NseIndiaService> logger) : INseIndiaService
{
    private readonly NseIndiaSettings _settings = options.Value;

    public async Task<NseIndiaEquityProfile?> GetEquityProfileAsync(
        string symbol,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(symbol))
        {
            return null;
        }

        symbol = GetBaseSymbol(symbol);
        try
        {
            var cookieHeader = await GetCookieHeaderAsync(cancellationToken);
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"api/quote-equity?symbol={Uri.EscapeDataString(symbol)}");
            AddNseHeaders(request, cookieHeader);

            using var response = await httpClient.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "NSE equity profile request failed for symbol {Symbol}. Status: {StatusCode}; Response: {ResponseBody}",
                    symbol,
                    (int)response.StatusCode,
                    Truncate(responseBody));
                return null;
            }

            using var document = JsonDocument.Parse(responseBody);
            var profile = TryReadProfile(document.RootElement, symbol);
            if (profile is null ||
                (string.IsNullOrWhiteSpace(profile.CompanyName) &&
                 string.IsNullOrWhiteSpace(profile.Industry)))
            {
                logger.LogWarning("NSE equity profile did not include usable data for symbol {Symbol}.", symbol);
                return null;
            }

            logger.LogInformation(
                "NSE equity profile returned company {CompanyName}, industry {Industry} for symbol {Symbol}.",
                profile.CompanyName,
                profile.Industry,
                symbol);

            return profile;
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "NSE equity profile request failed for symbol {Symbol}.", symbol);
            return null;
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "NSE equity profile response could not be parsed for symbol {Symbol}.", symbol);
            return null;
        }
    }

    private async Task<string?> GetCookieHeaderAsync(CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "");
        AddNseHeaders(request);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.Headers.TryGetValues("Set-Cookie", out var setCookieValues))
        {
            return null;
        }

        var cookies = setCookieValues
            .Select(value => value.Split(';', 2)[0].Trim())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return cookies.Length == 0 ? null : string.Join("; ", cookies);
    }

    private void AddNseHeaders(HttpRequestMessage request, string? cookieHeader = null)
    {
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue("en-US"));
        request.Headers.Referrer = new Uri(_settings.BaseUrl);
        if (!string.IsNullOrWhiteSpace(_settings.UserAgent))
        {
            request.Headers.UserAgent.ParseAdd(_settings.UserAgent);
        }

        if (!string.IsNullOrWhiteSpace(cookieHeader))
        {
            request.Headers.TryAddWithoutValidation("Cookie", cookieHeader);
        }
    }

    private static NseIndiaEquityProfile? TryReadProfile(JsonElement root, string requestedSymbol)
    {
        if (!root.TryGetProperty("info", out var info) ||
            info.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return new NseIndiaEquityProfile
        {
            Symbol = GetString(info, "symbol") ?? requestedSymbol,
            CompanyName = GetString(info, "companyName") ?? "",
            Industry = GetString(info, "industry") ?? ""
        };
    }

    private static string? GetString(JsonElement element, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (element.TryGetProperty(propertyName, out var property) &&
                property.ValueKind == JsonValueKind.String)
            {
                return property.GetString();
            }
        }

        return null;
    }

    private static string GetBaseSymbol(string symbol)
    {
        symbol = symbol.Trim().ToUpperInvariant();
        foreach (var suffix in new[] { "-EQ", "-BE", ".NS", ".BO" })
        {
            if (symbol.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                return symbol[..^suffix.Length];
            }
        }

        return symbol;
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
