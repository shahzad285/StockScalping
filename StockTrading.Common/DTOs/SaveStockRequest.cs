using StockTrading.Common.Enums;

namespace StockTrading.Common.DTOs;

public sealed class SaveStockRequest
{
    public int StockId { get; set; }
    public required string Symbol { get; set; }
    public string? Name { get; set; }
    public StockExchange Exchange { get; set; } = StockExchange.NSE;
    public string SymbolToken { get; set; } = "";
    public string TradingSymbol { get; set; } = "";
}
