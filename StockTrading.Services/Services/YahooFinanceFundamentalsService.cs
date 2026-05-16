using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StockTrading.Common.DTOs;
using StockTrading.Common.Settings;
using StockTrading.IServices;

namespace StockTrading.Services;

public sealed class YahooFinanceFundamentalsService(
    HttpClient httpClient,
    IOptions<YahooFinanceSettings> options,
    ILogger<YahooFinanceFundamentalsService> logger) : IYahooFinanceFundamentalsService
{
    private readonly YahooFinanceSettings _settings = options.Value;

    public async Task<YahooFinanceCompanyProfile?> GetCompanyProfileAsync(
        string symbol,
        string exchange,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(symbol))
        {
            return null;
        }

        var yahooSymbol = GetYahooSymbol(symbol, exchange);
        try
        {
            var session = await GetYahooSessionAsync(yahooSymbol, cancellationToken);
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"v10/finance/quoteSummary/{Uri.EscapeDataString(yahooSymbol)}?modules=summaryDetail,financialData,defaultKeyStatistics,incomeStatementHistory,balanceSheetHistory,cashflowStatementHistory,price,assetProfile{GetCrumbQuery(session.Crumb)}");
            AddYahooHeaders(request, session.CookieHeader);

            using var response = await httpClient.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Yahoo Finance company profile request failed for symbol {Symbol}. Status: {StatusCode}; Response: {ResponseBody}",
                    yahooSymbol,
                    (int)response.StatusCode,
                    Truncate(responseBody));
                return null;
            }

            using var document = JsonDocument.Parse(responseBody);
            if (TryGetYahooError(document.RootElement, out var errorCode, out var errorDescription))
            {
                logger.LogWarning(
                    "Yahoo Finance company profile response included an error for symbol {Symbol}. Code: {Code}; Description: {Description}",
                    yahooSymbol,
                    errorCode,
                    errorDescription);
                return null;
            }

            var profile = TryReadProfile(document.RootElement, yahooSymbol);
            if (profile is null ||
                (string.IsNullOrWhiteSpace(profile.Name) &&
                 string.IsNullOrWhiteSpace(profile.Sector) &&
                 string.IsNullOrWhiteSpace(profile.Industry) &&
                 profile.MarketCapitalization is null &&
                 profile.PERatio is null))
            {
                logger.LogWarning("Yahoo Finance company profile did not include usable fundamentals for symbol {Symbol}.", yahooSymbol);
                return null;
            }

            logger.LogInformation(
                "Yahoo Finance company profile returned name {Name}, sector {Sector}, industry {Industry}, market cap {MarketCap}, PE {PERatio} for symbol {Symbol}.",
                profile.Name,
                profile.Sector,
                profile.Industry,
                profile.MarketCapitalization,
                profile.PERatio,
                yahooSymbol);

            return profile;
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Yahoo Finance company profile request failed for symbol {Symbol}.", yahooSymbol);
            return null;
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Yahoo Finance company profile response could not be parsed for symbol {Symbol}.", yahooSymbol);
            return null;
        }
    }

    private async Task<YahooSession> GetYahooSessionAsync(
        string yahooSymbol,
        CancellationToken cancellationToken)
    {
        var cookieHeader = await GetCookieHeaderAsync(yahooSymbol, cancellationToken);
        var crumb = await GetCrumbAsync(cookieHeader, cancellationToken);
        return new YahooSession(cookieHeader, crumb);
    }

    private async Task<string?> GetCookieHeaderAsync(
        string yahooSymbol,
        CancellationToken cancellationToken)
    {
        var cookieBaseUrl = string.IsNullOrWhiteSpace(_settings.CookieBaseUrl)
            ? "https://finance.yahoo.com/"
            : _settings.CookieBaseUrl;

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            new Uri(new Uri(cookieBaseUrl), $"quote/{Uri.EscapeDataString(yahooSymbol)}"));
        AddYahooHeaders(request);

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

    private async Task<string?> GetCrumbAsync(
        string? cookieHeader,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "v1/test/getcrumb");
        AddYahooHeaders(request, cookieHeader);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var crumb = await response.Content.ReadAsStringAsync(cancellationToken);
        crumb = crumb.Trim();
        return string.IsNullOrWhiteSpace(crumb) || crumb.Contains('<', StringComparison.Ordinal)
            ? null
            : crumb;
    }

    private void AddYahooHeaders(HttpRequestMessage request, string? cookieHeader = null)
    {
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (!string.IsNullOrWhiteSpace(_settings.UserAgent))
        {
            request.Headers.UserAgent.ParseAdd(_settings.UserAgent);
        }

        if (!string.IsNullOrWhiteSpace(cookieHeader))
        {
            request.Headers.TryAddWithoutValidation("Cookie", cookieHeader);
        }
    }

    private static string GetCrumbQuery(string? crumb)
    {
        return string.IsNullOrWhiteSpace(crumb)
            ? ""
            : $"&crumb={Uri.EscapeDataString(crumb)}";
    }

    private static bool TryGetYahooError(
        JsonElement root,
        out string code,
        out string description)
    {
        code = "";
        description = "";
        if (!root.TryGetProperty("quoteSummary", out var quoteSummary) ||
            !quoteSummary.TryGetProperty("error", out var error) ||
            error.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return false;
        }

        code = GetString(error, "code") ?? "";
        description = GetString(error, "description") ?? "";
        return true;
    }

    private static YahooFinanceCompanyProfile? TryReadProfile(JsonElement root, string requestedSymbol)
    {
        if (!root.TryGetProperty("quoteSummary", out var quoteSummary) ||
            !quoteSummary.TryGetProperty("result", out var results) ||
            results.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var result = results.EnumerateArray().FirstOrDefault();
        if (result.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
        {
            return null;
        }

        var price = GetObject(result, "price");
        var assetProfile = GetObject(result, "assetProfile");
        var summaryDetail = GetObject(result, "summaryDetail");
        var defaultKeyStatistics = GetObject(result, "defaultKeyStatistics");
        var financialData = GetObject(result, "financialData");
        var latestIncomeStatement = GetLatestStatement(result, "incomeStatementHistory", "incomeStatementHistory");
        var latestBalanceSheet = GetLatestStatement(result, "balanceSheetHistory", "balanceSheetStatements");
        var latestCashFlowStatement = GetLatestStatement(result, "cashflowStatementHistory", "cashflowStatements");

        return new YahooFinanceCompanyProfile
        {
            Symbol = GetString(price, "symbol") ?? requestedSymbol,
            Name = GetString(price, "longName", "shortName") ?? "",
            Sector = GetString(assetProfile, "sector") ?? "",
            Industry = GetString(assetProfile, "industry") ?? "",
            Description = GetString(assetProfile, "longBusinessSummary") ?? "",
            MarketCapitalization = GetDecimal(price, "marketCap") ?? GetDecimal(summaryDetail, "marketCap"),
            PERatio = GetDecimal(defaultKeyStatistics, "trailingPE") ?? GetDecimal(summaryDetail, "trailingPE"),
            EarningsPerShare = GetDecimal(defaultKeyStatistics, "trailingEps") ?? GetDecimal(defaultKeyStatistics, "forwardEps"),
            PriceToBook = GetDecimal(defaultKeyStatistics, "priceToBook"),
            TotalRevenue = GetDecimal(financialData, "totalRevenue") ?? GetDecimal(latestIncomeStatement, "totalRevenue"),
            NetIncome = GetDecimal(latestIncomeStatement, "netIncome"),
            TotalDebt = GetDecimal(financialData, "totalDebt") ?? GetDecimal(latestBalanceSheet, "totalDebt"),
            TotalCash = GetDecimal(financialData, "totalCash") ?? GetDecimal(latestBalanceSheet, "cash"),
            CashFlow = GetDecimal(financialData, "freeCashflow") ?? GetDecimal(latestCashFlowStatement, "totalCashFromOperatingActivities"),
            DividendYield = GetDecimal(summaryDetail, "dividendYield"),
            DebtToEquity = GetDecimal(financialData, "debtToEquity")
        };
    }

    private static JsonElement? GetObject(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.Object
            ? value
            : null;
    }

    private static JsonElement? GetLatestStatement(JsonElement element, string moduleName, string arrayName)
    {
        var module = GetObject(element, moduleName);
        if (module is null ||
            !module.Value.TryGetProperty(arrayName, out var statements) ||
            statements.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var statement = statements.EnumerateArray().FirstOrDefault();
        return statement.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null
            ? null
            : statement;
    }

    private static string? GetString(JsonElement? element, params string[] propertyNames)
    {
        if (element is null)
        {
            return null;
        }

        foreach (var propertyName in propertyNames)
        {
            if (!element.Value.TryGetProperty(propertyName, out var property))
            {
                continue;
            }

            if (property.ValueKind == JsonValueKind.String)
            {
                return property.GetString();
            }

            if (property.ValueKind == JsonValueKind.Object &&
                property.TryGetProperty("raw", out var raw) &&
                raw.ValueKind == JsonValueKind.String)
            {
                return raw.GetString();
            }
        }

        return null;
    }

    private static decimal? GetDecimal(JsonElement? element, params string[] propertyNames)
    {
        if (element is null)
        {
            return null;
        }

        foreach (var propertyName in propertyNames)
        {
            if (!element.Value.TryGetProperty(propertyName, out var property))
            {
                continue;
            }

            var parsed = GetDecimalValue(property);
            if (parsed is not null)
            {
                return parsed;
            }
        }

        return null;
    }

    private static decimal? GetDecimalValue(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Number &&
            element.TryGetDecimal(out var number))
        {
            return number;
        }

        if (element.ValueKind == JsonValueKind.Object &&
            element.TryGetProperty("raw", out var raw))
        {
            return GetDecimalValue(raw);
        }

        if (element.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        var text = element.GetString();
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        text = text
            .Replace(",", "", StringComparison.Ordinal)
            .Replace("%", "", StringComparison.Ordinal)
            .Trim();

        return decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }

    private static string GetYahooSymbol(string symbol, string exchange)
    {
        symbol = GetBaseSymbol(symbol);
        exchange = string.IsNullOrWhiteSpace(exchange)
            ? "NSE"
            : exchange.Trim().ToUpperInvariant();

        return exchange == "BSE"
            ? $"{symbol}.BO"
            : $"{symbol}.NS";
    }

    private static string GetBaseSymbol(string symbol)
    {
        symbol = symbol.Trim().ToUpperInvariant();
        foreach (var suffix in new[] { "-EQ", "-BE" })
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

    private sealed record YahooSession(string? CookieHeader, string? Crumb);
}
