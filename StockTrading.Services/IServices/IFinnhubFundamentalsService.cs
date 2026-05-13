using StockTrading.Common.DTOs;

namespace StockTrading.IServices;

public interface IFinnhubFundamentalsService
{
    Task<FinnhubCompanyProfile?> GetCompanyProfileAsync(string symbol, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FinnhubSymbolSearchMatch>> SearchSymbolsAsync(string keywords, CancellationToken cancellationToken = default);
}
