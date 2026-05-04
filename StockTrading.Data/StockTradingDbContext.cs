using Microsoft.EntityFrameworkCore;
using StockTrading.Models;

namespace StockTrading.Data;

public class StockTradingDbContext(DbContextOptions<StockTradingDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderHistory> OrderHistories => Set<OrderHistory>();
    public DbSet<TrackedStock> TrackedStocks => Set<TrackedStock>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<TrackedStock>(entity =>
        {
            entity.ToTable("tracked_stocks");
            entity.HasKey(stock => stock.Symbol);

            entity.Property(stock => stock.Symbol)
                .HasColumnName("symbol")
                .HasMaxLength(100);

            entity.Property(stock => stock.Exchange)
                .HasColumnName("exchange")
                .HasMaxLength(20);

            entity.Property(stock => stock.SymbolToken)
                .HasColumnName("symbol_token")
                .HasMaxLength(100);

            entity.Property(stock => stock.TradingSymbol)
                .HasColumnName("trading_symbol")
                .HasMaxLength(100);

            entity.Property(stock => stock.PurchaseRate)
                .HasColumnName("purchase_rate")
                .HasPrecision(18, 4);

            entity.Property(stock => stock.SalesRate)
                .HasColumnName("sales_rate")
                .HasPrecision(18, 4);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("orders");
            entity.HasKey(order => order.Id);

            entity.Property(order => order.Id)
                .HasColumnName("id");

            entity.Property(order => order.BrokerOrderId)
                .HasColumnName("broker_order_id")
                .HasMaxLength(100);

            entity.Property(order => order.TradingSymbol)
                .HasColumnName("trading_symbol")
                .HasMaxLength(100);

            entity.Property(order => order.Exchange)
                .HasColumnName("exchange")
                .HasMaxLength(20);

            entity.Property(order => order.TransactionType)
                .HasColumnName("transaction_type")
                .HasMaxLength(30);

            entity.Property(order => order.OrderType)
                .HasColumnName("order_type")
                .HasMaxLength(30);

            entity.Property(order => order.ProductType)
                .HasColumnName("product_type")
                .HasMaxLength(30);

            entity.Property(order => order.Duration)
                .HasColumnName("duration")
                .HasMaxLength(30);

            entity.Property(order => order.Status)
                .HasColumnName("status")
                .HasConversion<string>()
                .HasMaxLength(50);

            entity.Property(order => order.RejectionReason)
                .HasColumnName("rejection_reason")
                .HasMaxLength(500);

            entity.Property(order => order.Quantity)
                .HasColumnName("quantity");

            entity.Property(order => order.FilledShares)
                .HasColumnName("filled_shares");

            entity.Property(order => order.UnfilledShares)
                .HasColumnName("unfilled_shares");

            entity.Property(order => order.CancelledShares)
                .HasColumnName("cancelled_shares");

            entity.Property(order => order.Price)
                .HasColumnName("price")
                .HasPrecision(18, 4);

            entity.Property(order => order.TriggerPrice)
                .HasColumnName("trigger_price")
                .HasPrecision(18, 4);

            entity.Property(order => order.AveragePrice)
                .HasColumnName("average_price")
                .HasPrecision(18, 4);

            entity.Property(order => order.UpdateTime)
                .HasColumnName("update_time")
                .HasMaxLength(100);

            entity.Property(order => order.ExchangeTime)
                .HasColumnName("exchange_time")
                .HasMaxLength(100);

            entity.Property(order => order.ParentBrokerOrderId)
                .HasColumnName("parent_broker_order_id")
                .HasMaxLength(100);

            entity.Property(order => order.CreatedAtUtc)
                .HasColumnName("created_at_utc");

            entity.Property(order => order.UpdatedAtUtc)
                .HasColumnName("updated_at_utc");

            entity.HasIndex(order => order.BrokerOrderId)
                .IsUnique();
        });

        modelBuilder.Entity<OrderHistory>(entity =>
        {
            entity.ToTable("order_histories");
            entity.HasKey(order => order.Id);

            entity.Property(order => order.Id)
                .HasColumnName("id");

            entity.Property(order => order.OrderId)
                .HasColumnName("order_id");

            entity.Property(order => order.EventType)
                .HasColumnName("event_type")
                .HasConversion<string>()
                .HasMaxLength(50);

            entity.Property(order => order.BrokerOrderId)
                .HasColumnName("broker_order_id")
                .HasMaxLength(100);

            entity.Property(order => order.TradingSymbol)
                .HasColumnName("trading_symbol")
                .HasMaxLength(100);

            entity.Property(order => order.Exchange)
                .HasColumnName("exchange")
                .HasMaxLength(20);

            entity.Property(order => order.TransactionType)
                .HasColumnName("transaction_type")
                .HasMaxLength(30);

            entity.Property(order => order.OrderType)
                .HasColumnName("order_type")
                .HasMaxLength(30);

            entity.Property(order => order.ProductType)
                .HasColumnName("product_type")
                .HasMaxLength(30);

            entity.Property(order => order.Duration)
                .HasColumnName("duration")
                .HasMaxLength(30);

            entity.Property(order => order.Status)
                .HasColumnName("status")
                .HasConversion<string>()
                .HasMaxLength(50);

            entity.Property(order => order.RejectionReason)
                .HasColumnName("rejection_reason")
                .HasMaxLength(500);

            entity.Property(order => order.Quantity)
                .HasColumnName("quantity");

            entity.Property(order => order.FilledShares)
                .HasColumnName("filled_shares");

            entity.Property(order => order.UnfilledShares)
                .HasColumnName("unfilled_shares");

            entity.Property(order => order.CancelledShares)
                .HasColumnName("cancelled_shares");

            entity.Property(order => order.Price)
                .HasColumnName("price")
                .HasPrecision(18, 4);

            entity.Property(order => order.TriggerPrice)
                .HasColumnName("trigger_price")
                .HasPrecision(18, 4);

            entity.Property(order => order.AveragePrice)
                .HasColumnName("average_price")
                .HasPrecision(18, 4);

            entity.Property(order => order.UpdateTime)
                .HasColumnName("update_time")
                .HasMaxLength(100);

            entity.Property(order => order.ExchangeTime)
                .HasColumnName("exchange_time")
                .HasMaxLength(100);

            entity.Property(order => order.ParentBrokerOrderId)
                .HasColumnName("parent_broker_order_id")
                .HasMaxLength(100);

            entity.Property(order => order.RecordedAtUtc)
                .HasColumnName("recorded_at_utc");

            entity.HasIndex(order => order.BrokerOrderId);
            entity.HasIndex(order => order.RecordedAtUtc);

            entity.HasOne(order => order.Order)
                .WithMany(order => order.History)
                .HasForeignKey(order => order.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
