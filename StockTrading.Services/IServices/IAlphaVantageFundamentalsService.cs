using StockTrading.Common.DTOs;

namespace StockTrading.IServices;

public interface IAlphaVantageFundamentalsService
{
    Task<AlphaVantageCompanyOverview?> GetCompanyOverviewAsync(string symbol, CancellationToken cancellationToken = default);
}
