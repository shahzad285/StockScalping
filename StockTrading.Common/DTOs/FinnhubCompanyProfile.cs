namespace StockTrading.Common.DTOs;

public sealed class FinnhubCompanyProfile
{
    public string Ticker { get; set; } = "";
    public string Name { get; set; } = "";
    public string FinnhubIndustry { get; set; } = "";
    public decimal? MarketCapitalization { get; set; }
    public string Exchange { get; set; } = "";
}
