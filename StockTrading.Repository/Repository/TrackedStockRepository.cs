using Dapper;
using StockTrading.Data;
using StockTrading.Models;
using StockTrading.Repository.IRepository;

namespace StockTrading.Repository.Repository;

public sealed class TrackedStockRepository(IDbConnectionFactory connectionFactory) : ITrackedStockRepository
{
    public async Task<IReadOnlyList<TrackedStock>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var stocks = await connection.QueryAsync<TrackedStock>(
            """
            select
                symbol as Symbol,
                exchange as Exchange,
                symbol_token as SymbolToken,
                trading_symbol as TradingSymbol,
                purchase_rate as PurchaseRate,
                sales_rate as SalesRate
            from tracked_stocks
            order by symbol
            """);

        return stocks.ToArray();
    }
}
