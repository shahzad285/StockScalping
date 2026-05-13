using Microsoft.Extensions.Logging;
using StockTrading.Common.DTOs;
using StockTrading.IServices;
using StockTrading.Repository.IRepository;

namespace StockTrading.Services;

public sealed class WatchlistService(
    IWatchlistRepository watchlistRepository,
    ILogger<WatchlistService> logger) : IWatchlistService
{
    public Task<IReadOnlyList<WatchlistStock>> GetStocksAsync(CancellationToken cancellationToken = default)
    {
        return watchlistRepository.GetAllAsync(cancellationToken);
    }

    public async Task<WatchlistStock> SaveStockAsync(WatchlistStock stock, CancellationToken cancellationToken = default)
    {
        var normalizedStock = Normalize(stock);
        logger.LogInformation(
            "Saving watchlist stock {Symbol} {TradingSymbol} with name {Name}.",
            normalizedStock.Symbol,
            normalizedStock.TradingSymbol,
            normalizedStock.Name ?? "<null>");

        await watchlistRepository.UpsertAsync(normalizedStock, cancellationToken);
        return normalizedStock;
    }

    public Task DeleteStockAsync(string symbol, CancellationToken cancellationToken = default)
    {
        return watchlistRepository.DeleteAsync(symbol.Trim().ToUpperInvariant(), cancellationToken);
    }

    public Task DeleteStockAsync(int watchlistId, CancellationToken cancellationToken = default)
    {
        return watchlistRepository.DeleteAsync(watchlistId, cancellationToken);
    }

    private static WatchlistStock Normalize(WatchlistStock stock)
    {
        if (string.IsNullOrWhiteSpace(stock.Symbol))
        {
            throw new ArgumentException("Symbol is required.");
        }

        if (string.IsNullOrWhiteSpace(stock.SymbolToken))
        {
            throw new ArgumentException("Symbol token is required.");
        }

        return new WatchlistStock
        {
            WatchlistId = stock.WatchlistId,
            StockId = stock.StockId,
            Symbol = stock.Symbol.Trim().ToUpperInvariant(),
            Name = GetStockName(stock.Name, stock.Symbol, stock.TradingSymbol),
            Exchange = string.IsNullOrWhiteSpace(stock.Exchange) ? "NSE" : stock.Exchange.Trim().ToUpperInvariant(),
            SymbolToken = stock.SymbolToken.Trim(),
            TradingSymbol = string.IsNullOrWhiteSpace(stock.TradingSymbol)
                ? stock.Symbol.Trim().ToUpperInvariant()
                : stock.TradingSymbol.Trim().ToUpperInvariant(),
            AssetType = string.IsNullOrWhiteSpace(stock.AssetType) ? "Unknown" : stock.AssetType.Trim(),
            Theme = string.IsNullOrWhiteSpace(stock.Theme) ? null : stock.Theme.Trim(),
            Sector = string.IsNullOrWhiteSpace(stock.Sector) ? null : stock.Sector.Trim(),
            Industry = string.IsNullOrWhiteSpace(stock.Industry) ? null : stock.Industry.Trim(),
            ClassificationReason = string.IsNullOrWhiteSpace(stock.ClassificationReason) ? null : stock.ClassificationReason.Trim(),
            ConfidenceScore = stock.ConfidenceScore
        };
    }

    private static string GetStockName(string? name, string symbol, string tradingSymbol)
    {
        if (!string.IsNullOrWhiteSpace(name))
        {
            return name.Trim();
        }

        var fallback = string.IsNullOrWhiteSpace(symbol) ? tradingSymbol : symbol;
        return GetDisplayName(fallback);
    }

    private static string GetDisplayName(string value)
    {
        var name = value.Trim().ToUpperInvariant();
        foreach (var suffix in new[] { "-EQ", "-BE" })
        {
            if (name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                return name[..^suffix.Length];
            }
        }

        return name;
    }
}
