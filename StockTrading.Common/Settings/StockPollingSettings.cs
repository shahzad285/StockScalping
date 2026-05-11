namespace StockTrading.Common.Settings;

public sealed class StockPollingSettings
{
    public bool Enabled { get; set; } = true;
    public int IntervalSeconds { get; set; } = 10;
}
