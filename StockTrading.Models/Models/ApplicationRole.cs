using Microsoft.AspNetCore.Identity;

namespace StockTrading.Models;

public class ApplicationRole : IdentityRole
{
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
