namespace StockTrading.Common.Settings;

public sealed class BrevoSettings
{
    public string ApiKey { get; set; } = "";
    public string FromEmail { get; set; } = "";
    public string FromName { get; set; } = "Stock Trading";
    public string Subject { get; set; } = "Your Stock Trading OTP";
}
