using StockTrading.Common.DTOs;
using StockTrading.IServices;
using StockTrading.Models;

namespace StockTrading.Services;

public sealed class OrderService(IBrokerService brokerService) : IOrderService
{
    public Task<List<OrderDetails>> GetOrdersAsync(CancellationToken cancellationToken = default)
    {
        return brokerService.GetOrdersAsync();
    }

    public async Task<OrderDetails?> GetOrderAsync(
        string brokerOrderId,
        CancellationToken cancellationToken = default)
    {
        var orders = await brokerService.GetOrdersAsync();
        return orders.FirstOrDefault(order =>
            string.Equals(order.OrderId, brokerOrderId, StringComparison.OrdinalIgnoreCase));
    }

    public Task<PlaceOrderResult> PlaceOrderAsync(
        PlaceOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        return brokerService.PlaceOrderAsync(request);
    }

    public Task<CancelOrderResult> CancelOrderAsync(
        string brokerOrderId,
        CancellationToken cancellationToken = default)
    {
        return brokerService.CancelOrderAsync(brokerOrderId);
    }

    public Task<List<OrderHistory>> GetHistoryAsync(
        string brokerOrderId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new List<OrderHistory>());
    }
}
