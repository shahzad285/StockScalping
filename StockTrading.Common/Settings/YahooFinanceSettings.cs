namespace StockTrading.Common.Settings;

public sealed class YahooFinanceSettings
{
    public string BaseUrl { get; set; } = "https://query1.finance.yahoo.com/";
    public string CookieBaseUrl { get; set; } = "https://finance.yahoo.com/";
    public string UserAgent { get; set; } = "Mozilla/5.0";
}
