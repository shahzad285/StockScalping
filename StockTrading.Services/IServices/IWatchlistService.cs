using StockTrading.Common.DTOs;

namespace StockTrading.IServices;

public interface IWatchlistService
{
    Task<IReadOnlyList<WatchlistStock>> GetStocksAsync(CancellationToken cancellationToken = default);
    Task<WatchlistStock> SaveStockAsync(WatchlistStock stock, CancellationToken cancellationToken = default);
    Task DeleteStockAsync(string symbol, CancellationToken cancellationToken = default);
    Task DeleteStockAsync(int watchlistId, CancellationToken cancellationToken = default);
}
