namespace StockTrading.IServices;

public interface IStockFundamentalsService
{
    Task<int> RefreshMissingProfilesAsync(int maxStocks, CancellationToken cancellationToken = default);
}
