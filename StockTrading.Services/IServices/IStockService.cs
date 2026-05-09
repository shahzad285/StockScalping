using StockTrading.Common.DTOs;
using StockTrading.Models;

namespace StockTrading.IServices;

public interface IStockService
{
    Task<HoldingsResponse> GetHoldingsAsync(CancellationToken cancellationToken = default);
    Task<List<StockPrice>> GetConfiguredPricesAsync(CancellationToken cancellationToken = default);
    Task<List<StockPrice>> GetPricesAsync(IEnumerable<TrackedStock> stocks, CancellationToken cancellationToken = default);
}
