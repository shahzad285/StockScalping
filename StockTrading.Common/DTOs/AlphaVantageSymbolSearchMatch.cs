namespace StockTrading.Common.DTOs;

public sealed class AlphaVantageSymbolSearchMatch
{
    public string Symbol { get; set; } = "";
    public string Name { get; set; } = "";
    public string Region { get; set; } = "";
    public string MatchScore { get; set; } = "";
}
