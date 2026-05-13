namespace StockTrading.Common.DTOs;

public sealed class FinnhubSymbolSearchMatch
{
    public string Symbol { get; set; } = "";
    public string DisplaySymbol { get; set; } = "";
    public string Description { get; set; } = "";
    public string Type { get; set; } = "";
}
