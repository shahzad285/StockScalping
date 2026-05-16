using StockTrading.Common.DTOs;
using StockTrading.Models;

namespace StockTrading.Repository.IRepository;

public interface IStockProfileRepository
{
    Task<IReadOnlyList<Stock>> GetStocksNeedingProfileAsync(int limit, CancellationToken cancellationToken = default);
    Task UpsertFundamentalsAsync(Stock stock, TapetideCompanyProfile profile, CancellationToken cancellationToken = default);
    Task UpsertFundamentalsAsync(Stock stock, YahooFinanceCompanyProfile profile, CancellationToken cancellationToken = default);
    Task UpsertFundamentalsAsync(Stock stock, NseIndiaEquityProfile profile, CancellationToken cancellationToken = default);
}
