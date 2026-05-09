namespace StockTrading.Models;

public class WatchlistStock
{
    public int WatchlistItemId { get; set; }
    public int WatchlistId { get; set; }
    public int StockId { get; set; }
    public required string Symbol { get; set; }
    public string Exchange { get; set; } = "NSE";
    public string SymbolToken { get; set; } = "";
    public string TradingSymbol { get; set; } = "";
    public decimal? PurchaseRate { get; set; }
    public decimal? SalesRate { get; set; }
}
