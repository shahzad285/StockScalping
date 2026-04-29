using StockScalping.Models;

namespace StockScalping.IServices;

public interface IAngelOneService
{
    /// <summary>
    /// Logs in to Angel One API
    /// </summary>
    /// <param name="totp">Optional TOTP for first-time login</param>
    /// <returns>True if login is successful</returns>
    Task<bool> Login(string totp = null);

    /// <summary>
    /// Gets the current price of a stock
    /// </summary>
    /// <param name="symbol">Stock symbol</param>
    /// <returns>Current stock price</returns>
    Task<decimal> GetCurrentPrice(string symbol);

    /// <summary>
    /// Places an order for a stock
    /// </summary>
    /// <param name="symbol">Stock symbol</param>
    /// <param name="quantity">Order quantity</param>
    /// <param name="orderType">BUY or SELL</param>
    /// <param name="price">Order price</param>
    /// <returns>True if order placement is successful</returns>
    Task<bool> PlaceOrder(string symbol, int quantity, string orderType, decimal price);

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
