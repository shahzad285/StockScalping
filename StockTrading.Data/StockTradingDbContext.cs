using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using StockTrading.Models;

namespace StockTrading.Data;

public class StockTradingDbContext(DbContextOptions<StockTradingDbContext> options)
    : IdentityDbContext<ApplicationUser, ApplicationRole, string>(options)
{
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderHistory> OrderHistories => Set<OrderHistory>();
    public DbSet<TrackedStock> TrackedStocks => Set<TrackedStock>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.ToTable("users");

            entity.Property(user => user.Id).HasColumnName("id");
            entity.Property(user => user.UserName).HasColumnName("user_name").HasMaxLength(256);
            entity.Property(user => user.NormalizedUserName).HasColumnName("normalized_user_name").HasMaxLength(256);
            entity.Property(user => user.Email).HasColumnName("email").HasMaxLength(256);
            entity.Property(user => user.NormalizedEmail).HasColumnName("normalized_email").HasMaxLength(256);
            entity.Property(user => user.EmailConfirmed).HasColumnName("email_confirmed");
            entity.Property(user => user.PasswordHash).HasColumnName("password_hash");
            entity.Property(user => user.SecurityStamp).HasColumnName("security_stamp");
            entity.Property(user => user.ConcurrencyStamp).HasColumnName("concurrency_stamp");
            entity.Property(user => user.PhoneNumber).HasColumnName("phone_number");
            entity.Property(user => user.PhoneNumberConfirmed).HasColumnName("phone_number_confirmed");
            entity.Property(user => user.TwoFactorEnabled).HasColumnName("two_factor_enabled");
            entity.Property(user => user.LockoutEnd).HasColumnName("lockout_end");
            entity.Property(user => user.LockoutEnabled).HasColumnName("lockout_enabled");
            entity.Property(user => user.AccessFailedCount).HasColumnName("access_failed_count");
            entity.Property(user => user.IsActive).HasColumnName("is_active");
            entity.Property(user => user.CreatedAtUtc).HasColumnName("created_at_utc");
        });

        modelBuilder.Entity<ApplicationRole>(entity =>
        {
            entity.ToTable("roles");

            entity.Property(role => role.Id).HasColumnName("id");
            entity.Property(role => role.Name).HasColumnName("name").HasMaxLength(256);
            entity.Property(role => role.NormalizedName).HasColumnName("normalized_name").HasMaxLength(256);
            entity.Property(role => role.ConcurrencyStamp).HasColumnName("concurrency_stamp");
            entity.Property(role => role.CreatedAtUtc).HasColumnName("created_at_utc");
        });

        modelBuilder.Entity<IdentityUserRole<string>>(entity =>
        {
            entity.ToTable("user_roles");

            entity.Property(userRole => userRole.UserId).HasColumnName("user_id");
            entity.Property(userRole => userRole.RoleId).HasColumnName("role_id");
        });

        modelBuilder.Entity<IdentityUserClaim<string>>(entity =>
        {
            entity.ToTable("user_claims");

            entity.Property(userClaim => userClaim.Id).HasColumnName("id");
            entity.Property(userClaim => userClaim.UserId).HasColumnName("user_id");
            entity.Property(userClaim => userClaim.ClaimType).HasColumnName("claim_type");
            entity.Property(userClaim => userClaim.ClaimValue).HasColumnName("claim_value");
        });

        modelBuilder.Entity<IdentityRoleClaim<string>>(entity =>
        {
            entity.ToTable("role_claims");

            entity.Property(roleClaim => roleClaim.Id).HasColumnName("id");
            entity.Property(roleClaim => roleClaim.RoleId).HasColumnName("role_id");
            entity.Property(roleClaim => roleClaim.ClaimType).HasColumnName("claim_type");
            entity.Property(roleClaim => roleClaim.ClaimValue).HasColumnName("claim_value");
        });

        modelBuilder.Entity<IdentityUserLogin<string>>(entity =>
        {
            entity.ToTable("user_logins");

            entity.Property(userLogin => userLogin.LoginProvider).HasColumnName("login_provider");
            entity.Property(userLogin => userLogin.ProviderKey).HasColumnName("provider_key");
            entity.Property(userLogin => userLogin.ProviderDisplayName).HasColumnName("provider_display_name");
            entity.Property(userLogin => userLogin.UserId).HasColumnName("user_id");
        });

        modelBuilder.Entity<IdentityUserToken<string>>(entity =>
        {
            entity.ToTable("user_tokens");

            entity.Property(userToken => userToken.UserId).HasColumnName("user_id");
            entity.Property(userToken => userToken.LoginProvider).HasColumnName("login_provider");
            entity.Property(userToken => userToken.Name).HasColumnName("name");
            entity.Property(userToken => userToken.Value).HasColumnName("value");
        });

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
