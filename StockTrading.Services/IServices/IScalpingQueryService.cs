using StockTrading.Models;

namespace StockTrading.IServices;

public interface IScalpingQueryService
{
    Task<ScalpingStatusResponse> GetStatusAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TrackedStock>> GetStocksAsync(CancellationToken cancellationToken = default);
}

public sealed record ScalpingStatusResponse(
    bool IsConfigured,
    int TrackedStocks,
    int TradableStocks);
