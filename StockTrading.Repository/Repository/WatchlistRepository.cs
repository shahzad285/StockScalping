using Dapper;
using StockTrading.Common.DTOs;
using StockTrading.Data;
using StockTrading.Repository.IRepository;

namespace StockTrading.Repository.Repository;

public sealed class WatchlistRepository(IDbConnectionFactory connectionFactory) : IWatchlistRepository
{
    public async Task<IReadOnlyList<WatchlistStock>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var stocks = await connection.QueryAsync<WatchlistStock>(
            """
            select
                watchlist.id as WatchlistId,
                stocks.id as StockId,
                stocks.symbol as Symbol,
                stocks.name as Name,
                stocks.exchange as Exchange,
                stocks.symbol_token as SymbolToken,
                stocks.trading_symbol as TradingSymbol,
                coalesce(stock_profiles.asset_type, 'Unknown') as AssetType,
                stock_profiles.theme as Theme,
                stock_profiles.sector as Sector,
                stock_profiles.industry as Industry,
                stock_profiles.classification_reason as ClassificationReason,
                stock_profiles.confidence_score as ConfidenceScore
            from watchlist
            join stocks
              on stocks.id = watchlist.stock_id
            left join stock_profiles
              on stock_profiles.stock_id = stocks.id
            where watchlist.is_active = true
            order by stocks.symbol
            """);

        return stocks.ToArray();
    }

    public async Task UpsertAsync(WatchlistStock stock, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(
            """
            with saved_stock as (
                insert into stocks (
                    symbol,
                    name,
                    exchange,
                    symbol_token,
                    trading_symbol,
                    created_at_utc
                )
                values (
                    @Symbol,
                    @Name,
                    @Exchange,
                    @SymbolToken,
                    @TradingSymbol,
                    now()
                )
                on conflict (exchange, symbol_token) do update
                set symbol = excluded.symbol,
                    name = coalesce(excluded.name, stocks.name),
                    trading_symbol = excluded.trading_symbol,
                    updated_at_utc = now()
                returning id
            ),
            saved_profile as (
                insert into stock_profiles (
                    stock_id,
                    asset_type,
                    theme,
                    sector,
                    industry,
                    classification_reason,
                    confidence_score,
                    created_at_utc
                )
                select
                    saved_stock.id,
                    @AssetType,
                    @Theme,
                    @Sector,
                    @Industry,
                    @ClassificationReason,
                    @ConfidenceScore,
                    now()
                from saved_stock
                on conflict (stock_id) do update
                set asset_type = excluded.asset_type,
                    theme = excluded.theme,
                    sector = excluded.sector,
                    industry = excluded.industry,
                    classification_reason = excluded.classification_reason,
                    confidence_score = excluded.confidence_score,
                    updated_at_utc = now()
                returning stock_id
            )
            insert into watchlist (
                stock_id,
                is_active,
                created_at_utc
            )
            select
                saved_profile.stock_id,
                true,
                now()
            from saved_profile
            on conflict (stock_id) do update
            set is_active = true,
                updated_at_utc = now()
            """,
            new
            {
                stock.Symbol,
                stock.Name,
                stock.Exchange,
                stock.SymbolToken,
                stock.TradingSymbol,
                stock.AssetType,
                stock.Theme,
                stock.Sector,
                stock.Industry,
                stock.ClassificationReason,
                stock.ConfidenceScore
            });
    }

    public async Task DeleteAsync(string symbol, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(
            """
            update watchlist
            set is_active = false,
                updated_at_utc = now()
            from stocks
            where watchlist.stock_id = stocks.id
              and stocks.symbol = @Symbol
            """,
            new { Symbol = symbol });
    }

    public async Task DeleteAsync(int watchlistId, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(
            """
            update watchlist
            set is_active = false,
                updated_at_utc = now()
            where id = @WatchlistId
            """,
            new { WatchlistId = watchlistId });
    }
}
