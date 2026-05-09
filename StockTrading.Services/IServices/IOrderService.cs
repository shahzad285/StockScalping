using StockTrading.Common.DTOs;
using StockTrading.Models;

namespace StockTrading.IServices;

public interface IOrderService
{
    Task<List<OrderDetails>> GetOrdersAsync(CancellationToken cancellationToken = default);
    Task<OrderDetails?> GetOrderAsync(string brokerOrderId, CancellationToken cancellationToken = default);
    Task<PlaceOrderResult> PlaceOrderAsync(PlaceOrderRequest request, CancellationToken cancellationToken = default);
    Task<CancelOrderResult> CancelOrderAsync(string brokerOrderId, CancellationToken cancellationToken = default);
    Task<List<OrderHistory>> GetHistoryAsync(string brokerOrderId, CancellationToken cancellationToken = default);
}
