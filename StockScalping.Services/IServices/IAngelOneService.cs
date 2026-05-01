using StockScalping.Models;

namespace StockScalping.IServices;

public interface IAngelOneService : IBrokerService
{
    /// <summary>
    /// Logs in to Angel One API
    /// </summary>
    /// <param name="totp">Optional TOTP for first-time login</param>
    /// <returns>True if login is successful</returns>
    Task<bool> Login(string? totp = null);

    /// <summary>
    /// Gets the logged-in Angel One account profile
    /// </summary>
    /// <returns>Account profile details if authenticated</returns>
    Task<AccountProfile?> GetProfile();

    /// <summary>
    /// Gets real-time LTP prices for the configured stock list
    /// </summary>
    /// <returns>List of stocks with latest traded prices</returns>
    Task<List<StockPrice>> GetConfiguredStockPrices();

    /// <summary>
    /// Gets real-time LTP prices for a stock list
    /// </summary>
    /// <param name="stocks">Stocks to fetch</param>
    /// <returns>List of stocks with latest traded prices</returns>
    Task<List<StockPrice>> GetCurrentPrices(IEnumerable<StockProfile> stocks);

    /// <summary>
    /// Gets all stocks held in the account
    /// </summary>
    /// <returns>List of stocks with purchase price, quantity, current price, and gain/loss</returns>
    Task<List<HoldingStock>> GetHoldingStocks();

    /// <summary>
    /// Gets all orders from the broker order book, including executed, rejected, cancelled, and pending/open orders
    /// </summary>
    /// <returns>List of orders with status and execution details</returns>
    Task<List<OrderDetails>> GetOrders();
}
