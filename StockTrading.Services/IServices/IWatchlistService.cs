using StockTrading.Common.DTOs;
using StockTrading.Models;

namespace StockTrading.IServices;

public interface IWatchlistService
{
    Task<IReadOnlyList<Watchlist>> GetWatchlistsAsync(CancellationToken cancellationToken = default);
    Task<Watchlist> CreateWatchlistAsync(string name, CancellationToken cancellationToken = default);
    Task DeleteWatchlistAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WatchlistStock>> GetStocksAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WatchlistStock>> GetStocksAsync(int watchlistId, CancellationToken cancellationToken = default);
    Task<WatchlistStock> SaveStockAsync(WatchlistStock stock, CancellationToken cancellationToken = default);
    Task<WatchlistStock> SaveStockAsync(int watchlistId, WatchlistStock stock, CancellationToken cancellationToken = default);
    Task DeleteStockAsync(string symbol, CancellationToken cancellationToken = default);
    Task DeleteStockAsync(int watchlistId, int watchlistItemId, CancellationToken cancellationToken = default);
}
