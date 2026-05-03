using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StockTrading.Models;
using StockTrading.IServices;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace StockTrading.Services;

public class ScalpingService : BackgroundService, IScalpingService
{
    private readonly ILogger<ScalpingService> _logger;
    private readonly IBrokerService _brokerService;
    private readonly List<StockProfile> _stocks;

    public ScalpingService(ILogger<ScalpingService> logger, 
                          IBrokerService brokerService,
                          IConfiguration config)
    {
        _logger = logger;
        _brokerService = brokerService;
        
        // Load configured stocks from appsettings.json
        _stocks = config.GetSection("Trading:Stocks").Get<List<StockProfile>>() 
                 ?? new List<StockProfile>();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            foreach (var stock in _stocks)
            {
                if (!stock.PurchaseRate.HasValue || !stock.SalesRate.HasValue)
                {
                    continue;
                }

                var currentPrice = await _brokerService.GetCurrentPrice(stock.Symbol);
                
                if (currentPrice <= stock.PurchaseRate.Value)
                {
                    _logger.LogInformation($"Buy condition met for {stock.Symbol} at {currentPrice}");
                    await _brokerService.PlaceOrder(stock.Symbol, 1, "BUY", currentPrice);
                }
                else if (currentPrice >= stock.SalesRate.Value)
                {
                    _logger.LogInformation($"Sell condition met for {stock.Symbol} at {currentPrice}");
                    await _brokerService.PlaceOrder(stock.Symbol, 1, "SELL", currentPrice);
                }
            }
            
            await Task.Delay(5000, stoppingToken); // Check every 5 seconds
        }
    }
}
