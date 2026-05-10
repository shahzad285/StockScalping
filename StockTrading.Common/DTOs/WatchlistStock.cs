namespace StockTrading.Common.DTOs;

public class WatchlistStock
{
    public int WatchlistId { get; set; }
    public int StockId { get; set; }
    public required string Symbol { get; set; }
    public string Exchange { get; set; } = "NSE";
    public string SymbolToken { get; set; } = "";
    public string TradingSymbol { get; set; } = "";
    public decimal? PurchaseRate { get; set; }
    public decimal? SalesRate { get; set; }
    public string AssetType { get; set; } = "Unknown";
    public string? Theme { get; set; }
    public string? Sector { get; set; }
    public string? Industry { get; set; }
    public string? ClassificationReason { get; set; }
    public decimal? ConfidenceScore { get; set; }
}
