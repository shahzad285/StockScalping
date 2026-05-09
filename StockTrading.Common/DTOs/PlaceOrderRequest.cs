namespace StockTrading.Common.DTOs;

public sealed record PlaceOrderRequest(
    string Symbol,
    string Exchange,
    string TransactionType,
    string OrderType,
    string ProductType,
    string Duration,
    int Quantity,
    decimal Price,
    decimal? TriggerPrice = null,
    string? SymbolToken = null,
    string? TradingSymbol = null);
