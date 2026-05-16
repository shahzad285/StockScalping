namespace StockTrading.Common.Settings;

public sealed class MarketScheduleSettings
{
    public string TimeZoneId { get; set; } = "India Standard Time";
    public string OpenTime { get; set; } = "09:00";
    public string CloseTime { get; set; } = "15:30";
}
