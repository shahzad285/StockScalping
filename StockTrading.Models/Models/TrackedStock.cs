namespace StockTrading.Models;

/// <summary>
/// Stock/instrument configured for tracking or trading.
/// </summary>
public class TrackedStock
{
    public required string Symbol { get; set; }
    public string Exchange { get; set; } = "NSE";
    public string SymbolToken { get; set; } = "";
    public string TradingSymbol { get; set; } = "";
    public decimal? PurchaseRate { get; set; }
    public decimal? SalesRate { get; set; }
}
