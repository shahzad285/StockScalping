namespace StockTrading.Common.DTOs;

/// <summary>
/// Latest live price returned by SmartAPI for a configured stock/instrument.
/// </summary>
public class StockPrice
{
    public string Symbol { get; set; } = "";
    public string TradingSymbol { get; set; } = "";
    public string Exchange { get; set; } = "";
    public string SymbolToken { get; set; } = "";
    public decimal LastTradedPrice { get; set; }
    public bool IsFetched { get; set; }
    public string Message { get; set; } = "";
}
