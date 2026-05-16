using Dapper;
using StockTrading.Common.DTOs;
using StockTrading.Data;
using StockTrading.Repository.IRepository;

namespace StockTrading.Repository.Repository;

public sealed class WatchlistDataRepository(IDbConnectionFactory connectionFactory) : IWatchlistDataRepository
{
    public async Task UpsertDailyPricesAsync(
        IEnumerable<WatchlistStock> stocks,
        IEnumerable<StockPrice> prices,
        DateOnly tradingDate,
        DateTime sampleTimeUtc,
        CancellationToken cancellationToken = default)
    {
        var stockList = stocks.ToArray();
        var priceList = prices
            .Where(price => price.IsFetched && price.LastTradedPrice > 0)
            .ToArray();

        if (stockList.Length == 0 || priceList.Length == 0)
        {
            return;
        }

        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        foreach (var price in priceList)
        {
            var stock = stockList.FirstOrDefault(item =>
                item.WatchlistId > 0 &&
                item.StockId > 0 &&
                string.Equals(item.Exchange, price.Exchange, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(item.SymbolToken, price.SymbolToken, StringComparison.OrdinalIgnoreCase));
            if (stock == null)
            {
                continue;
            }

            await connection.ExecuteAsync(
                """
                insert into watchlist_data (
                    watchlist_id,
                    stock_id,
                    trading_date,
                    day_low,
                    day_high,
                    average_price,
                    price_sample_count,
                    final_price,
                    first_price_at_utc,
                    last_price_at_utc,
                    created_at_utc
                )
                values (
                    @WatchlistId,
                    @StockId,
                    @TradingDate,
                    @Price,
                    @Price,
                    @Price,
                    1,
                    @Price,
                    @SampleTimeUtc,
                    @SampleTimeUtc,
                    now()
                )
                on conflict (stock_id, trading_date) do update
                set watchlist_id = excluded.watchlist_id,
                    day_low = least(coalesce(watchlist_data.day_low, excluded.day_low), excluded.day_low),
                    day_high = greatest(coalesce(watchlist_data.day_high, excluded.day_high), excluded.day_high),
                    average_price = case
                        when coalesce(watchlist_data.price_sample_count, 0) <= 0 then excluded.average_price
                        else (
                            (coalesce(watchlist_data.average_price, excluded.average_price) *
                             watchlist_data.price_sample_count) + excluded.average_price
                        ) / (watchlist_data.price_sample_count + 1)
                    end,
                    price_sample_count = coalesce(watchlist_data.price_sample_count, 0) + 1,
                    final_price = excluded.final_price,
                    first_price_at_utc = coalesce(watchlist_data.first_price_at_utc, excluded.first_price_at_utc),
                    last_price_at_utc = excluded.last_price_at_utc,
                    updated_at_utc = now()
                """,
                new
                {
                    stock.WatchlistId,
                    stock.StockId,
                    TradingDate = tradingDate.ToDateTime(TimeOnly.MinValue),
                    Price = price.LastTradedPrice,
                    SampleTimeUtc = sampleTimeUtc
                });
        }
    }
}
