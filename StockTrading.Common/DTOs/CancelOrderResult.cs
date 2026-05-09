namespace StockTrading.Common.DTOs;

public sealed record CancelOrderResult(
    bool IsSuccess,
    string? BrokerOrderId = null,
    string? Message = null);
