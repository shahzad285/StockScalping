namespace StockTrading.Models;

public class WatchlistData
{
    public int Id { get; set; }
    public int WatchlistId { get; set; }
    public int StockId { get; set; }
    public DateOnly TradingDate { get; set; }
    public decimal? DailyLow { get; set; }
    public decimal? DailyHigh { get; set; }
    public decimal? FinalPrice { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
}
