namespace StockTrading.Common.Settings;

public sealed class FinnhubSettings
{
    public string BaseUrl { get; set; } = "https://finnhub.io/api/v1/";
    public string ApiKey { get; set; } = "";
}
