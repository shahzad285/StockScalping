using Microsoft.Extensions.Configuration;
using StockTrading.Common.DTOs;
using StockTrading.IServices;
using StockTrading.Models;

namespace StockTrading.Services;

public sealed class StockService(
    IConfiguration configuration,
    IBrokerService brokerService) : IStockService
{
    public Task<HoldingsResponse> GetHoldingsAsync(CancellationToken cancellationToken = default)
    {
        return brokerService.GetHoldingsAsync();
    }

    public Task<List<StockPrice>> GetConfiguredPricesAsync(CancellationToken cancellationToken = default)
    {
        var stocks = configuration.GetSection("Trading:Stocks").Get<List<TrackedStock>>() ?? new List<TrackedStock>();
        return brokerService.GetPricesAsync(stocks);
    }

    public Task<List<StockPrice>> GetPricesAsync(
        IEnumerable<TrackedStock> stocks,
        CancellationToken cancellationToken = default)
    {
        return brokerService.GetPricesAsync(stocks);
    }
}
