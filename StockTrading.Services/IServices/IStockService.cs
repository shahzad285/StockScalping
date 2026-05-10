using StockTrading.Common.DTOs;
using StockTrading.Models;

namespace StockTrading.IServices;

public interface IStockService
{
    Task<HoldingsResponse> GetHoldingsAsync(CancellationToken cancellationToken = default);
    Task<List<StockSearchResult>> SearchStocksAsync(
        string query,
        StockExchange exchange = StockExchange.NSE,
        CancellationToken cancellationToken = default);
    Task<List<StockCandle>> GetCandlesAsync(
        string symbolToken,
        StockExchange exchange = StockExchange.NSE,
        StockChartInterval interval = StockChartInterval.ONE_DAY,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default);
    Task<List<StockPrice>> GetConfiguredPricesAsync(CancellationToken cancellationToken = default);
    Task<List<StockPrice>> GetPricesAsync(IEnumerable<WatchlistStock> stocks, CancellationToken cancellationToken = default);
}
