namespace StockTrading.Models;

/// <summary>
/// Configured stock/instrument to track before requesting live market data.
/// </summary>
public class StockProfile
{
    public required string Symbol { get; set; }
    public string Exchange { get; set; } = "NSE";
    public string SymbolToken { get; set; } = "";
    public string TradingSymbol { get; set; } = "";
    public decimal? PurchaseRate { get; set; }
    public decimal? SalesRate { get; set; }
}
