namespace StockTrading.Models;

public class WatchlistData
{
    public int Id { get; set; }
    public int WatchlistId { get; set; }
    public int StockId { get; set; }
    public DateOnly TradingDate { get; set; }
    public decimal? DayLow { get; set; }
    public decimal? DayHigh { get; set; }
    public decimal? AveragePrice { get; set; }
    public int PriceSampleCount { get; set; }
    public decimal? FinalPrice { get; set; }
    public DateTime? FirstPriceAtUtc { get; set; }
    public DateTime? LastPriceAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
}
