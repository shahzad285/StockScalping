namespace StockTrading.Models;

public class MarketJobDecision
{
    public int Id { get; set; }
    public DateOnly DecisionDate { get; set; }
    public string Exchange { get; set; } = "NSE";
    public string JobName { get; set; } = "";
    public bool IsTradingDay { get; set; }
    public TimeOnly MarketOpenTime { get; set; }
    public TimeOnly MarketCloseTime { get; set; }
    public bool JobsEnabled { get; set; }
    public string? DecisionReason { get; set; }
    public DateTime DecidedAtUtc { get; set; }
    public DateTime? JobsDisabledUntilUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
}
