using StockTrading.Common.DTOs;
using StockTrading.Models;

namespace StockTrading.Repository.IRepository;

public interface IStockRepository
{
    Task<IReadOnlyList<WatchlistStock>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Stock> UpsertAsync(SaveStockRequest request, CancellationToken cancellationToken = default);
    Task<Stock?> GetByIdAsync(int stockId, CancellationToken cancellationToken = default);
    Task<StockDeleteCheck> GetDeleteCheckAsync(int stockId, CancellationToken cancellationToken = default);
    Task DeleteAsync(int stockId, CancellationToken cancellationToken = default);
}
