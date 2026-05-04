using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace StockTrading.Data;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddStockTradingData(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("StockTrading")
            ?? throw new InvalidOperationException("Connection string 'StockTrading' is missing.");

        services.AddDbContext<StockTradingDbContext>(options =>
            options.UseNpgsql(connectionString));

        return services;
    }
}
