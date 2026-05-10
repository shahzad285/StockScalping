namespace StockTrading.Common.DTOs;

public sealed class StockSearchResult
{
    public string Symbol { get; set; } = "";
    public string TradingSymbol { get; set; } = "";
    public string Exchange { get; set; } = "";
    public string SymbolToken { get; set; } = "";
    public string? Name { get; set; }
}
