namespace StockTrading.Common.DTOs;

public sealed class MarketJobDecisionResult
{
    public DateOnly DecisionDate { get; set; }
    public string Exchange { get; set; } = "NSE";
    public string JobName { get; set; } = "";
    public bool IsTradingDay { get; set; }
    public bool JobsEnabled { get; set; }
    public TimeOnly MarketOpenTime { get; set; }
    public TimeOnly MarketCloseTime { get; set; }
    public DateTime? JobsDisabledUntilUtc { get; set; }
    public string DecisionReason { get; set; } = "";
}
