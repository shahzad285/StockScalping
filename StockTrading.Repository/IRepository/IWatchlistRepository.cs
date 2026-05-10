using StockTrading.Common.DTOs;

namespace StockTrading.Repository.IRepository;

public interface IWatchlistRepository
{
    Task<IReadOnlyList<WatchlistStock>> GetAllAsync(CancellationToken cancellationToken = default);
    Task UpsertAsync(WatchlistStock stock, CancellationToken cancellationToken = default);
    Task DeleteAsync(string symbol, CancellationToken cancellationToken = default);
    Task DeleteAsync(int watchlistId, CancellationToken cancellationToken = default);
}
