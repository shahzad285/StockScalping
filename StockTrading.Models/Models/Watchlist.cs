namespace StockTrading.Models;

public class Watchlist
{
    public int Id { get; set; }
    public int StockId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
}
