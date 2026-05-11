using StockTrading.IServices;
using StockTrading.Models;
using StockTrading.Repository.IRepository;

namespace StockTrading.Services;

public sealed class StockFundamentalsService(
    IStockProfileRepository stockProfileRepository,
    IAlphaVantageFundamentalsService alphaVantageFundamentalsService) : IStockFundamentalsService
{
    public async Task<int> RefreshMissingProfilesAsync(
        int maxStocks,
        CancellationToken cancellationToken = default)
    {
        var stocks = await stockProfileRepository.GetStocksNeedingProfileAsync(maxStocks, cancellationToken);
        var updatedCount = 0;

        foreach (var stock in stocks)
        {
            var overview = await alphaVantageFundamentalsService.GetCompanyOverviewAsync(
                GetAlphaVantageSymbol(stock),
                cancellationToken);

            if (overview == null)
            {
                continue;
            }

            await stockProfileRepository.UpsertFundamentalsAsync(stock, overview, cancellationToken);
            updatedCount++;
        }

        return updatedCount;
    }

    private static string GetAlphaVantageSymbol(Stock stock)
    {
        var symbol = string.IsNullOrWhiteSpace(stock.Symbol)
            ? stock.TradingSymbol
            : stock.Symbol;

        symbol = symbol.Trim().ToUpperInvariant();
        if (symbol.EndsWith("-EQ", StringComparison.OrdinalIgnoreCase))
        {
            symbol = symbol[..^3];
        }

        if (symbol.Contains('.'))
        {
            return symbol;
        }

        var exchange = string.IsNullOrWhiteSpace(stock.Exchange)
            ? "NSE"
            : stock.Exchange.Trim().ToUpperInvariant();

        return exchange == "BSE"
            ? $"{symbol}.BSE"
            : $"{symbol}.NSE";
    }
}
