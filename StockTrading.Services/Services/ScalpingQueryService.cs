using StockTrading.IServices;
using StockTrading.Models;
using StockTrading.Repository.IRepository;

namespace StockTrading.Services;

public sealed class ScalpingQueryService(ITrackedStockRepository trackedStockRepository) : IScalpingQueryService
{
    public async Task<ScalpingStatusResponse> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var stocks = await trackedStockRepository.GetAllAsync(cancellationToken);
        return new ScalpingStatusResponse(
            stocks.Count > 0,
            stocks.Count,
            stocks.Count(stock => stock.PurchaseRate.HasValue && stock.SalesRate.HasValue));
    }

    public Task<IReadOnlyList<TrackedStock>> GetStocksAsync(CancellationToken cancellationToken = default)
    {
        return trackedStockRepository.GetAllAsync(cancellationToken);
    }
}
