namespace StockTrading.Common.DTOs;

public sealed record PlaceOrderResult(
    bool IsSuccess,
    string? BrokerOrderId = null,
    string? Message = null);
