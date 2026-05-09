using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StockTrading.Common.DTOs;
using StockTrading.Models;
using StockTrading.IServices;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace StockTrading.Services;

public class ScalpingService : BackgroundService, IScalpingService
{
    private readonly ILogger<ScalpingService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly List<TrackedStock> _stocks;

    public ScalpingService(ILogger<ScalpingService> logger, 
                          IServiceScopeFactory scopeFactory,
                          IConfiguration config)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        
        // Load configured stocks from appsettings.json
        _stocks = config.GetSection("Trading:Stocks").Get<List<TrackedStock>>() 
                 ?? new List<TrackedStock>();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _scopeFactory.CreateScope();
            var brokerService = scope.ServiceProvider.GetRequiredService<IBrokerService>();

            foreach (var stock in _stocks)
            {
                if (!stock.PurchaseRate.HasValue || !stock.SalesRate.HasValue)
                {
                    continue;
                }

                var prices = await brokerService.GetPricesAsync(new[] { stock });
                var currentPrice = prices.FirstOrDefault()?.LastTradedPrice ?? 0m;
                
                if (currentPrice <= stock.PurchaseRate.Value)
                {
                    _logger.LogInformation($"Buy condition met for {stock.Symbol} at {currentPrice}");
                    await brokerService.PlaceOrderAsync(CreatePlaceOrderRequest(stock, "BUY", currentPrice));
                }
                else if (currentPrice >= stock.SalesRate.Value)
                {
                    _logger.LogInformation($"Sell condition met for {stock.Symbol} at {currentPrice}");
                    await brokerService.PlaceOrderAsync(CreatePlaceOrderRequest(stock, "SELL", currentPrice));
                }
            }
            
            await Task.Delay(5000, stoppingToken); // Check every 5 seconds
        }
    }

    private static PlaceOrderRequest CreatePlaceOrderRequest(
        TrackedStock stock,
        string transactionType,
        decimal price)
    {
        return new PlaceOrderRequest(
            stock.Symbol,
            string.IsNullOrWhiteSpace(stock.Exchange) ? "NSE" : stock.Exchange,
            transactionType,
            "MARKET",
            "INTRADAY",
            "DAY",
            1,
            price,
            SymbolToken: stock.SymbolToken,
            TradingSymbol: stock.TradingSymbol);
    }
}
