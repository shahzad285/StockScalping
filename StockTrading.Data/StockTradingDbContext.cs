using Microsoft.EntityFrameworkCore;

namespace StockTrading.Data;

public class StockTradingDbContext(DbContextOptions<StockTradingDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
}
