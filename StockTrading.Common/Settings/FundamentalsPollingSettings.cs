namespace StockTrading.Common.Settings;

public sealed class FundamentalsPollingSettings
{
    public bool Enabled { get; set; } = false;
    public int IntervalMinutes { get; set; } = 60;
    public int MaxStocksPerRun { get; set; } = 3;
}
