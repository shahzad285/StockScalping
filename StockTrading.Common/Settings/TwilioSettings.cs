namespace StockTrading.Common.Settings;

public sealed class TwilioSettings
{
    public string AccountSid { get; set; } = "";
    public string AuthToken { get; set; } = "";
    public string FromPhoneNumber { get; set; } = "";
}
