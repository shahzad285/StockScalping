using StockTrading.Common.DTOs;
using StockTrading.Models;

namespace StockTrading.IServices;

public interface IBrokerService
{
    Task<bool> LoginAsync(string? otp = null);
    Task<AccountProfile?> GetProfileAsync();
    Task<HoldingsResponse> GetHoldingsAsync();
    Task<List<StockPrice>> GetPricesAsync(IEnumerable<WatchlistStock> stocks);
    Task<List<OrderDetails>> GetOrdersAsync();
    Task<PlaceOrderResult> PlaceOrderAsync(PlaceOrderRequest request);
    Task<CancelOrderResult> CancelOrderAsync(string brokerOrderId);
}
