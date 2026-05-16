using StockTrading.Common.DTOs;

namespace StockTrading.Repository.IRepository;

public interface IWatchlistDataRepository
{
    Task UpsertDailyPricesAsync(
        IEnumerable<WatchlistStock> stocks,
        IEnumerable<StockPrice> prices,
        DateOnly tradingDate,
        DateTime sampleTimeUtc,
        CancellationToken cancellationToken = default);
}
