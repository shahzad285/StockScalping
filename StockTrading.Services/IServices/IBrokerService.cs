namespace StockTrading.IServices;

public interface IBrokerService
{
    /// <summary>
    /// Gets the current price of a stock.
    /// </summary>
    /// <param name="symbol">Stock symbol</param>
    /// <returns>Current stock price</returns>
    Task<decimal> GetCurrentPrice(string symbol);

    /// <summary>
    /// Places an order for a stock.
    /// </summary>
    /// <param name="symbol">Stock symbol</param>
    /// <param name="quantity">Order quantity</param>
    /// <param name="orderType">BUY or SELL</param>
    /// <param name="price">Order price</param>
    /// <returns>True if order placement is successful</returns>
    Task<bool> PlaceOrder(string symbol, int quantity, string orderType, decimal price);
}
