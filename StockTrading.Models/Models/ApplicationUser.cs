namespace StockTrading.Models;

public class ApplicationUser
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string MobileNumber { get; set; } = "";
    public string NormalizedMobileNumber { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
