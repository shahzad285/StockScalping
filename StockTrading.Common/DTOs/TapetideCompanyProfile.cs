namespace StockTrading.Common.DTOs;

public sealed class TapetideCompanyProfile
{
    public string Symbol { get; set; } = "";
    public string Name { get; set; } = "";
    public string Sector { get; set; } = "";
    public string Industry { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal? MarketCapitalization { get; set; }
    public decimal? PERatio { get; set; }
    public decimal? DividendYield { get; set; }
    public decimal? DebtToEquity { get; set; }
}
