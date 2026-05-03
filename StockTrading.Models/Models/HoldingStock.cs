namespace StockTrading.Models;

public class HoldingStock
{
    public string StockName { get; set; } = "";
    public string TradingSymbol { get; set; } = "";
    public string Exchange { get; set; } = "";
    public string SymbolToken { get; set; } = "";
    public decimal PurchasePrice { get; set; }
    public int TotalStocks { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal TotalGainOrLoss => (TotalStocks * CurrentPrice) - (TotalStocks * PurchasePrice);
}
