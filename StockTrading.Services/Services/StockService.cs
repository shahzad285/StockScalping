using StockTrading.Common.DTOs;
using StockTrading.IServices;
using StockTrading.Models;
using StockTrading.Repository.IRepository;

namespace StockTrading.Services;

public sealed class StockService(
    IBrokerService brokerService,
    IWatchlistRepository watchlistRepository) : IStockService
{
    public Task<HoldingsResponse> GetHoldingsAsync(CancellationToken cancellationToken = default)
    {
        return brokerService.GetHoldingsAsync();
    }

    public Task<List<StockSearchResult>> SearchStocksAsync(
        string query,
        StockExchange exchange = StockExchange.NSE,
        CancellationToken cancellationToken = default)
    {
        return brokerService.SearchStocksAsync(query, exchange);
    }

    public async Task<List<StockPrice>> GetConfiguredPricesAsync(CancellationToken cancellationToken = default)
    {
        var stocks = await watchlistRepository.GetAllAsync(cancellationToken);
        return await brokerService.GetPricesAsync(stocks);
    }

    public Task<List<StockPrice>> GetPricesAsync(
        IEnumerable<WatchlistStock> stocks,
        CancellationToken cancellationToken = default)
    {
        return brokerService.GetPricesAsync(stocks);
    }
}
