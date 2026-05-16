using StockTrading.Common.DTOs;

namespace StockTrading.Repository.IRepository;

public interface IMarketJobDecisionRepository
{
    Task UpsertAsync(
        MarketJobDecisionResult decision,
        CancellationToken cancellationToken = default);
}
