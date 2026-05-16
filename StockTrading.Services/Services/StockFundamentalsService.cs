using Microsoft.Extensions.Logging;
using StockTrading.Common.DTOs;
using StockTrading.IServices;
using StockTrading.Models;
using StockTrading.Repository.IRepository;

namespace StockTrading.Services;

public sealed class StockFundamentalsService(
    IStockProfileRepository stockProfileRepository,
    IYahooFinanceFundamentalsService yahooFinanceFundamentalsService,
    INseIndiaService nseIndiaService,
    ITapetideFundamentalsService tapetideFundamentalsService,
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
            var stockUpdated = false;

            if (IsNseStock(stock))
            {
                logger.LogInformation(
                    "Fetching fundamentals for stock {StockId} {Symbol} {TradingSymbol} using NSE India equity profile.",
                    stock.Id,
                    stock.Symbol,
                    stock.TradingSymbol);

                var nseIndiaProfile = await GetNseIndiaProfileAsync(stock, cancellationToken);
                if (nseIndiaProfile != null)
                {
                    await stockProfileRepository.UpsertFundamentalsAsync(stock, nseIndiaProfile, cancellationToken);
                    logger.LogInformation(
                        "Saved NSE India profile for stock {StockId} {Symbol}. Company: {CompanyName}; Industry: {Industry}.",
                        stock.Id,
                        stock.Symbol,
                        nseIndiaProfile.CompanyName,
                        nseIndiaProfile.Industry);

                    stockUpdated = true;
                }
                else
                {
                    logger.LogWarning(
                        "No NSE India equity profile returned for stock {StockId} {Symbol}.",
                        stock.Id,
                        stock.Symbol);
                }
            }

            logger.LogInformation(
                "Fetching fundamentals for stock {StockId} {Symbol} {TradingSymbol} using Yahoo Finance company profile.",
                stock.Id,
                stock.Symbol,
                stock.TradingSymbol);

            var yahooFinanceProfile = await GetYahooFinanceProfileAsync(stock, cancellationToken);
            if (yahooFinanceProfile != null)
            {
                await stockProfileRepository.UpsertFundamentalsAsync(stock, yahooFinanceProfile, cancellationToken);
                logger.LogInformation(
                    "Saved Yahoo Finance fundamentals for stock {StockId} {Symbol}. Sector: {Sector}; Industry: {Industry}; MarketCap: {MarketCap}; PERatio: {PERatio}.",
                    stock.Id,
                    stock.Symbol,
                    yahooFinanceProfile.Sector,
                    yahooFinanceProfile.Industry,
                    yahooFinanceProfile.MarketCapitalization,
                    yahooFinanceProfile.PERatio);

                stockUpdated = true;
                if (!await stockProfileRepository.HasMissingFundamentalsAsync(stock.Id, cancellationToken))
                {
                    updatedCount++;
                    continue;
                }
            }
            else
            {
                logger.LogWarning(
                    "No Yahoo Finance company profile returned for stock {StockId} {Symbol}.",
                    stock.Id,
                    stock.Symbol);
            }

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

                stockUpdated = true;
            }

            if (tapetideProfile == null)
            {
                logger.LogWarning(
                    "No Tapetide company profile returned for stock {StockId} {Symbol}.",
                    stock.Id,
                    stock.Symbol);
            }

            if (stockUpdated)
            {
                updatedCount++;
            }
        }

        return updatedCount;
    }

    private async Task<YahooFinanceCompanyProfile?> GetYahooFinanceProfileAsync(
        Stock stock,
        CancellationToken cancellationToken)
    {
        var symbol = GetBaseSymbol(stock);
        var exchange = string.IsNullOrWhiteSpace(stock.Exchange)
            ? "NSE"
            : stock.Exchange.Trim().ToUpperInvariant();

        logger.LogInformation(
            "Trying Yahoo Finance company profile for stock {StockId} {Symbol} using base symbol {YahooFinanceSymbol} and exchange {Exchange}.",
            stock.Id,
            stock.Symbol,
            symbol,
            exchange);

        return await yahooFinanceFundamentalsService.GetCompanyProfileAsync(symbol, exchange, cancellationToken);
    }

    private async Task<NseIndiaEquityProfile?> GetNseIndiaProfileAsync(
        Stock stock,
        CancellationToken cancellationToken)
    {
        var symbol = GetBaseSymbol(stock);

        logger.LogInformation(
            "Trying NSE India equity profile for stock {StockId} {Symbol} using base symbol {NseIndiaSymbol}.",
            stock.Id,
            stock.Symbol,
            symbol);

        return await nseIndiaService.GetEquityProfileAsync(symbol, cancellationToken);
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

    private static bool IsNseStock(Stock stock)
    {
        return string.IsNullOrWhiteSpace(stock.Exchange) ||
            stock.Exchange.Trim().Equals("NSE", StringComparison.OrdinalIgnoreCase);
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
