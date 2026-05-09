namespace StockTrading.Models;

public class WatchlistItem
{
    public int Id { get; set; }
    public int WatchlistId { get; set; }
    public int StockId { get; set; }
    public decimal? BuyTargetPrice { get; set; }
    public decimal? SellTargetPrice { get; set; }
    public decimal? LastPrice { get; set; }
    public int BuyTargetHitCount { get; set; }
    public int SellTargetHitCount { get; set; }
    public DateTime? LastBuyTargetHitAtUtc { get; set; }
    public DateTime? LastSellTargetHitAtUtc { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
}
