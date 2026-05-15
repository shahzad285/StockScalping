using Microsoft.Extensions.Logging;
using StockTrading.Common.DTOs;
using StockTrading.IServices;
using StockTrading.Models;
using StockTrading.Repository.IRepository;

namespace StockTrading.Services;

public sealed class StockFundamentalsService(
    IStockProfileRepository stockProfileRepository,
    ITapetideFundamentalsService tapetideFundamentalsService,
    IAlphaVantageFundamentalsService alphaVantageFundamentalsService,
    IFinnhubFundamentalsService finnhubFundamentalsService,
    ILogger<StockFundamentalsService> logger) : IStockFundamentalsService
{
    public async Task<int> RefreshMissingProfilesAsync(
        int maxStocks,
        CancellationToken cancellationToken = default)
    {
        var stocks = await stockProfileRepository.GetStocksNeedingProfileAsync(maxStocks, cancellationToken);
        var updatedCount = 0;

        foreach (var stock in stocks)
        {
            logger.LogInformation(
                "Fetching fundamentals for stock {StockId} {Symbol} {TradingSymbol} using Tapetide company profile.",
                stock.Id,
                stock.Symbol,
                stock.TradingSymbol);

            var tapetideProfile = await GetTapetideProfileAsync(stock, cancellationToken);
            if (tapetideProfile != null)
            {
                await stockProfileRepository.UpsertFundamentalsAsync(stock, tapetideProfile, cancellationToken);
                logger.LogInformation(
                    "Saved Tapetide fundamentals for stock {StockId} {Symbol}. Sector: {Sector}; Industry: {Industry}; MarketCap: {MarketCap}; PERatio: {PERatio}.",
                    stock.Id,
                    stock.Symbol,
                    tapetideProfile.Sector,
                    tapetideProfile.Industry,
                    tapetideProfile.MarketCapitalization,
                    tapetideProfile.PERatio);

                updatedCount++;
                continue;
            }

            logger.LogInformation(
                "Fetching fundamentals for stock {StockId} {Symbol} {TradingSymbol} using Alpha Vantage symbol search.",
                stock.Id,
                stock.Symbol,
                stock.TradingSymbol);

            var overview = await GetAlphaVantageOverviewFromSearchAsync(stock, cancellationToken);

            if (overview != null)
            {
                await stockProfileRepository.UpsertFundamentalsAsync(stock, overview, cancellationToken);
                logger.LogInformation(
                    "Saved Alpha Vantage fundamentals for stock {StockId} {Symbol}. Sector: {Sector}; Industry: {Industry}; MarketCap: {MarketCap}; PERatio: {PERatio}.",
                    stock.Id,
                    stock.Symbol,
                    overview.Sector,
                    overview.Industry,
                    overview.MarketCapitalization,
                    overview.PERatio);

                updatedCount++;
                continue;
            }

            logger.LogInformation(
                "Falling back to Finnhub fundamentals for stock {StockId} {Symbol} using symbol search.",
                stock.Id,
                stock.Symbol);

            var finnhubProfile = await GetFinnhubProfileFromSearchAsync(stock, cancellationToken);
            if (finnhubProfile == null)
            {
                logger.LogWarning(
                    "No Finnhub company profile returned for stock {StockId} {Symbol} using symbol search.",
                    stock.Id,
                    stock.Symbol);

                continue;
            }

            await stockProfileRepository.UpsertFundamentalsAsync(stock, finnhubProfile, cancellationToken);
            logger.LogInformation(
                "Saved Finnhub fundamentals for stock {StockId} {Symbol}. Name: {Name}; Industry: {Industry}; MarketCap: {MarketCap}.",
                stock.Id,
                stock.Symbol,
                finnhubProfile.Name,
                finnhubProfile.FinnhubIndustry,
                finnhubProfile.MarketCapitalization);

            updatedCount++;
        }

        return updatedCount;
    }

    private async Task<TapetideCompanyProfile?> GetTapetideProfileAsync(
        Stock stock,
        CancellationToken cancellationToken)
    {
        var symbol = GetBaseSymbol(stock);

        logger.LogInformation(
            "Trying Tapetide company profile for stock {StockId} {Symbol} using base symbol {TapetideSymbol}.",
            stock.Id,
            stock.Symbol,
            symbol);

        return await tapetideFundamentalsService.GetCompanyProfileAsync(symbol, cancellationToken);
    }

    private async Task<AlphaVantageCompanyOverview?> GetAlphaVantageOverviewFromSearchAsync(
        Stock stock,
        CancellationToken cancellationToken)
    {
        var keywords = string.IsNullOrWhiteSpace(stock.Name)
            ? GetBaseSymbol(stock)
            : stock.Name.Trim();

        logger.LogInformation(
            "Searching Alpha Vantage symbols for stock {StockId} {Symbol} using keywords {Keywords}.",
            stock.Id,
            stock.Symbol,
            keywords);

        var matches = await alphaVantageFundamentalsService.SearchSymbolsAsync(keywords, cancellationToken);
        var match = matches.FirstOrDefault(item => IsMatchingAlphaVantageExchange(item, stock));
        if (match == null)
        {
            logger.LogWarning(
                "Alpha Vantage symbol search returned no exchange-matching symbol for stock {StockId} {Symbol} using keywords {Keywords}.",
                stock.Id,
                stock.Symbol,
                keywords);
            return null;
        }

        logger.LogInformation(
            "Trying Alpha Vantage overview for stock {StockId} {Symbol} using searched symbol {AlphaVantageSymbol} ({Name}, {Region}).",
            stock.Id,
            stock.Symbol,
            match.Symbol,
            match.Name,
            match.Region);

        return await alphaVantageFundamentalsService.GetCompanyOverviewAsync(match.Symbol, cancellationToken);
    }

    private async Task<FinnhubCompanyProfile?> GetFinnhubProfileFromSearchAsync(
        Stock stock,
        CancellationToken cancellationToken)
    {
        var keywords = string.IsNullOrWhiteSpace(stock.Name)
            ? GetBaseSymbol(stock)
            : stock.Name.Trim();

        logger.LogInformation(
            "Searching Finnhub symbols for stock {StockId} {Symbol} using keywords {Keywords}.",
            stock.Id,
            stock.Symbol,
            keywords);

        var matches = await finnhubFundamentalsService.SearchSymbolsAsync(keywords, cancellationToken);
        var match = matches.FirstOrDefault(item => IsMatchingFinnhubExchange(item, stock));
        if (match == null)
        {
            logger.LogWarning(
                "Finnhub symbol search returned no exchange-matching symbol for stock {StockId} {Symbol} using keywords {Keywords}.",
                stock.Id,
                stock.Symbol,
                keywords);
            return null;
        }

        logger.LogInformation(
            "Trying Finnhub company profile for stock {StockId} {Symbol} using searched symbol {FinnhubSymbol} ({Description}).",
            stock.Id,
            stock.Symbol,
            match.Symbol,
            match.Description);

        return await finnhubFundamentalsService.GetCompanyProfileAsync(match.Symbol, cancellationToken);
    }

    private static bool IsMatchingAlphaVantageExchange(AlphaVantageSymbolSearchMatch match, Stock stock)
    {
        var exchange = string.IsNullOrWhiteSpace(stock.Exchange)
            ? "NSE"
            : stock.Exchange.Trim().ToUpperInvariant();

        return exchange == "BSE"
            ? match.Symbol.EndsWith(".BSE", StringComparison.OrdinalIgnoreCase)
            : match.Symbol.EndsWith(".NSE", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsMatchingFinnhubExchange(FinnhubSymbolSearchMatch match, Stock stock)
    {
        var exchange = string.IsNullOrWhiteSpace(stock.Exchange)
            ? "NSE"
            : stock.Exchange.Trim().ToUpperInvariant();

        return exchange == "BSE"
            ? match.Symbol.EndsWith(".BO", StringComparison.OrdinalIgnoreCase)
            : match.Symbol.EndsWith(".NS", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetBaseSymbol(Stock stock)
    {
        var symbol = string.IsNullOrWhiteSpace(stock.Symbol)
            ? stock.TradingSymbol
            : stock.Symbol;

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
}
