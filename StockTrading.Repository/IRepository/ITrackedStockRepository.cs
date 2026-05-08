using StockTrading.Models;

namespace StockTrading.Repository.IRepository;

public interface ITrackedStockRepository
{
    Task<IReadOnlyList<TrackedStock>> GetAllAsync(CancellationToken cancellationToken = default);
}
