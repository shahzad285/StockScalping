namespace StockTrading.Common.DTOs;

public sealed class NseIndiaEquityProfile
{
    public string Symbol { get; set; } = "";
    public string CompanyName { get; set; } = "";
    public string Industry { get; set; } = "";
    public decimal? MarketCapitalization { get; set; }
    public decimal? PERatio { get; set; }
}
