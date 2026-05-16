using StockTrading.Common.DTOs;

namespace StockTrading.IServices;

public interface IMarketScheduleService
{
    Task<MarketJobDecisionResult> DecideAsync(
        string jobName,
        string exchange = "NSE",
        CancellationToken cancellationToken = default);
}
