namespace StockTrading.Common.Settings;

public sealed class OtpSettings
{
    public int ExpiryMinutes { get; set; } = 5;
    public bool ExposeOtpInResponse { get; set; }
}
