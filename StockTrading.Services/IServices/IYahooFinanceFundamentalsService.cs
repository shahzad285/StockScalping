using StockTrading.Common.DTOs;

namespace StockTrading.IServices;

public interface IYahooFinanceFundamentalsService
{
    Task<YahooFinanceCompanyProfile?> GetCompanyProfileAsync(
        string symbol,
        string exchange,
        CancellationToken cancellationToken = default);
}
