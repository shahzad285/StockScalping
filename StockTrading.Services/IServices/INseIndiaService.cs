using StockTrading.Common.DTOs;

namespace StockTrading.IServices;

public interface INseIndiaService
{
    Task<NseIndiaEquityProfile?> GetEquityProfileAsync(
        string symbol,
        CancellationToken cancellationToken = default);
}
