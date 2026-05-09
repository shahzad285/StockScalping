namespace StockTrading.Models;

public class BrokerSessionRecord
{
    public int Id { get; set; }
    public string BrokerName { get; set; } = "";
    public int? UserId { get; set; }
    public string AccessTokenEncrypted { get; set; } = "";
    public string RefreshTokenEncrypted { get; set; } = "";
    public string? FeedTokenEncrypted { get; set; }
    public DateTime? AccessTokenExpiresAtUtc { get; set; }
    public DateTime? RefreshTokenExpiresAtUtc { get; set; }
    public string? RawDataJson { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
}
