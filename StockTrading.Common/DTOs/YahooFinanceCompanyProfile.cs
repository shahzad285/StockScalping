namespace StockTrading.Common.DTOs;

public sealed class YahooFinanceCompanyProfile
{
    public string Symbol { get; set; } = "";
    public string Name { get; set; } = "";
    public string Sector { get; set; } = "";
    public string Industry { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal? MarketCapitalization { get; set; }
    public decimal? PERatio { get; set; }
    public decimal? EarningsPerShare { get; set; }
    public decimal? PriceToBook { get; set; }
    public decimal? TotalRevenue { get; set; }
    public decimal? NetIncome { get; set; }
    public decimal? TotalDebt { get; set; }
    public decimal? TotalCash { get; set; }
    public decimal? CashFlow { get; set; }
    public decimal? DividendYield { get; set; }
    public decimal? DebtToEquity { get; set; }
}
