using StockTrading.Common.DTOs;
using StockTrading.Models;

namespace StockTrading.Repository.IRepository;

public interface IStockProfileRepository
{
    Task<IReadOnlyList<Stock>> GetStocksNeedingProfileAsync(int limit, CancellationToken cancellationToken = default);
    Task UpsertFundamentalsAsync(Stock stock, AlphaVantageCompanyOverview overview, CancellationToken cancellationToken = default);
    Task UpsertFundamentalsAsync(Stock stock, FinnhubCompanyProfile profile, CancellationToken cancellationToken = default);
}
