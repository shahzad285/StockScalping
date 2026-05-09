using StockTrading.IServices;
using StockTrading.Models;
using StockTrading.Repository.IRepository;

namespace StockTrading.Services;

public sealed class WatchlistService(IWatchlistRepository watchlistRepository) : IWatchlistService
{
    public Task<IReadOnlyList<Watchlist>> GetWatchlistsAsync(CancellationToken cancellationToken = default)
    {
        return watchlistRepository.GetWatchlistsAsync(cancellationToken);
    }

    public Task<Watchlist> CreateWatchlistAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Watchlist name is required.");
        }

        return watchlistRepository.CreateWatchlistAsync(name.Trim(), cancellationToken);
    }

    public Task DeleteWatchlistAsync(int id, CancellationToken cancellationToken = default)
    {
        return watchlistRepository.DeleteWatchlistAsync(id, cancellationToken);
    }

    public Task<IReadOnlyList<WatchlistStock>> GetStocksAsync(CancellationToken cancellationToken = default)
    {
        return watchlistRepository.GetAllAsync(cancellationToken);
    }

    public Task<IReadOnlyList<WatchlistStock>> GetStocksAsync(int watchlistId, CancellationToken cancellationToken = default)
    {
        return watchlistRepository.GetStocksAsync(watchlistId, cancellationToken);
    }

    public async Task<WatchlistStock> SaveStockAsync(WatchlistStock stock, CancellationToken cancellationToken = default)
    {
        var normalizedStock = Normalize(stock);
        await watchlistRepository.UpsertAsync(normalizedStock, cancellationToken);
        return normalizedStock;
    }

    public async Task<WatchlistStock> SaveStockAsync(int watchlistId, WatchlistStock stock, CancellationToken cancellationToken = default)
    {
        var normalizedStock = Normalize(stock);
        await watchlistRepository.UpsertAsync(watchlistId, normalizedStock, cancellationToken);
        return normalizedStock;
    }

    public Task DeleteStockAsync(string symbol, CancellationToken cancellationToken = default)
    {
        return watchlistRepository.DeleteAsync(symbol.Trim().ToUpperInvariant(), cancellationToken);
    }

    public Task DeleteStockAsync(int watchlistId, int watchlistItemId, CancellationToken cancellationToken = default)
    {
        return watchlistRepository.DeleteStockAsync(watchlistId, watchlistItemId, cancellationToken);
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
            Symbol = stock.Symbol.Trim().ToUpperInvariant(),
            Exchange = string.IsNullOrWhiteSpace(stock.Exchange) ? "NSE" : stock.Exchange.Trim().ToUpperInvariant(),
            SymbolToken = stock.SymbolToken.Trim(),
            TradingSymbol = string.IsNullOrWhiteSpace(stock.TradingSymbol)
                ? stock.Symbol.Trim().ToUpperInvariant()
                : stock.TradingSymbol.Trim().ToUpperInvariant(),
            PurchaseRate = stock.PurchaseRate,
            SalesRate = stock.SalesRate
        };
    }
}
