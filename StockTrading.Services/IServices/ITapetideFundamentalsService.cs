using StockTrading.Common.DTOs;

namespace StockTrading.IServices;

public interface ITapetideFundamentalsService
{
    Task<TapetideCompanyProfile?> GetCompanyProfileAsync(
        string symbol,
        CancellationToken cancellationToken = default);
}
