namespace StockTrading.Models;

public class TradePlanRun
{
    public int Id { get; set; }
    public int TradePlanId { get; set; }
    public string Status { get; set; } = TradePlanRunStatuses.WaitingToBuy;
    public string? BuyOrderId { get; set; }
    public string? SellOrderId { get; set; }
    public decimal? BuyPrice { get; set; }
    public decimal? SellPrice { get; set; }
    public int Quantity { get; set; }
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public static class TradePlanRunStatuses
{
    public const string WaitingToBuy = "WaitingToBuy";
    public const string BuyPlaced = "BuyPlaced";
    public const string Bought = "Bought";
    public const string WaitingToSell = "WaitingToSell";
    public const string SellPlaced = "SellPlaced";
    public const string Completed = "Completed";
    public const string Cancelled = "Cancelled";
    public const string Failed = "Failed";
}
