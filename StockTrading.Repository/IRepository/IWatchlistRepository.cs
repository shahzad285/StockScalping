using StockTrading.Common.DTOs;
using StockTrading.Models;

namespace StockTrading.Repository.IRepository;

public interface IWatchlistRepository
{
    Task<IReadOnlyList<Watchlist>> GetWatchlistsAsync(CancellationToken cancellationToken = default);
    Task<Watchlist> CreateWatchlistAsync(string name, CancellationToken cancellationToken = default);
    Task DeleteWatchlistAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WatchlistStock>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WatchlistStock>> GetStocksAsync(int watchlistId, CancellationToken cancellationToken = default);
    Task UpsertAsync(WatchlistStock stock, CancellationToken cancellationToken = default);
    Task UpsertAsync(int watchlistId, WatchlistStock stock, CancellationToken cancellationToken = default);
    Task DeleteAsync(string symbol, CancellationToken cancellationToken = default);
    Task DeleteStockAsync(int watchlistId, int watchlistItemId, CancellationToken cancellationToken = default);
}
