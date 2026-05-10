using Dapper;
using StockTrading.Data;
using StockTrading.Common.DTOs;
using StockTrading.Models;
using StockTrading.Repository.IRepository;

namespace StockTrading.Repository.Repository;

public sealed class WatchlistRepository(IDbConnectionFactory connectionFactory) : IWatchlistRepository
{
    private const string DefaultWatchlistName = "Default";

    public async Task<IReadOnlyList<Watchlist>> GetWatchlistsAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var watchlists = await connection.QueryAsync<Watchlist>(
            """
            select
                id as Id,
                name as Name,
                is_active as IsActive,
                created_at_utc as CreatedAtUtc,
                updated_at_utc as UpdatedAtUtc
            from watchlists
            where is_active = true
            order by name
            """);

        return watchlists.ToArray();
    }

    public async Task<Watchlist> CreateWatchlistAsync(string name, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleAsync<Watchlist>(
            """
            insert into watchlists (name, is_active, created_at_utc)
            values (@Name, true, now())
            on conflict (name) do update
            set is_active = true,
                updated_at_utc = now()
            returning
                id as Id,
                name as Name,
                is_active as IsActive,
                created_at_utc as CreatedAtUtc,
                updated_at_utc as UpdatedAtUtc
            """,
            new { Name = name });
    }

    public async Task DeleteWatchlistAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(
            """
            update watchlists
            set is_active = false,
                updated_at_utc = now()
            where id = @Id
            """,
            new { Id = id });
    }

    public async Task<IReadOnlyList<WatchlistStock>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var stocks = await connection.QueryAsync<WatchlistStock>(
            """
            select
                stocks.symbol as Symbol,
                stocks.exchange as Exchange,
                stocks.symbol_token as SymbolToken,
                stocks.trading_symbol as TradingSymbol,
                watchlist_items.buy_target_price as PurchaseRate,
                watchlist_items.sell_target_price as SalesRate
            from watchlist_items
            join watchlists
              on watchlists.id = watchlist_items.watchlist_id
            join stocks
              on stocks.id = watchlist_items.stock_id
            where watchlists.name = @WatchlistName
              and watchlist_items.is_active = true
            order by stocks.symbol
            """,
            new { WatchlistName = DefaultWatchlistName });

        return stocks.ToArray();
    }

    public async Task<IReadOnlyList<WatchlistStock>> GetStocksAsync(int watchlistId, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var stocks = await connection.QueryAsync<WatchlistStock>(
            """
            select
                watchlist_items.id as WatchlistItemId,
                watchlist_items.watchlist_id as WatchlistId,
                stocks.id as StockId,
                stocks.symbol as Symbol,
                stocks.exchange as Exchange,
                stocks.symbol_token as SymbolToken,
                stocks.trading_symbol as TradingSymbol,
                watchlist_items.buy_target_price as PurchaseRate,
                watchlist_items.sell_target_price as SalesRate
            from watchlist_items
            join stocks
              on stocks.id = watchlist_items.stock_id
            where watchlist_items.watchlist_id = @WatchlistId
              and watchlist_items.is_active = true
            order by stocks.symbol
            """,
            new { WatchlistId = watchlistId });

        return stocks.ToArray();
    }

    public async Task UpsertAsync(WatchlistStock stock, CancellationToken cancellationToken = default)
    {
        await UpsertAsync(DefaultWatchlistName, stock, cancellationToken);
    }

    public async Task UpsertAsync(int watchlistId, WatchlistStock stock, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(
            """
            with saved_stock as (
                insert into stocks (
                    symbol,
                    exchange,
                    symbol_token,
                    trading_symbol,
                    created_at_utc
                )
                values (
                    @Symbol,
                    @Exchange,
                    @SymbolToken,
                    @TradingSymbol,
                    now()
                )
                on conflict (exchange, symbol_token) do update
                set symbol = excluded.symbol,
                    trading_symbol = excluded.trading_symbol,
                    updated_at_utc = now()
                returning id
            )
            insert into watchlist_items (
                watchlist_id,
                stock_id,
                buy_target_price,
                sell_target_price,
                is_active,
                created_at_utc
            )
            select
                @WatchlistId,
                saved_stock.id,
                @PurchaseRate,
                @SalesRate,
                true,
                now()
            from saved_stock
            on conflict (watchlist_id, stock_id) do update
            set buy_target_price = excluded.buy_target_price,
                sell_target_price = excluded.sell_target_price,
                is_active = true,
                updated_at_utc = now()
            """,
            new
            {
                WatchlistId = watchlistId,
                stock.Symbol,
                stock.Exchange,
                stock.SymbolToken,
                stock.TradingSymbol,
                stock.PurchaseRate,
                stock.SalesRate
            });
    }

    private async Task UpsertAsync(string watchlistName, WatchlistStock stock, CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(
            """
            insert into watchlists (name, is_active, created_at_utc)
            values (@WatchlistName, true, now())
            on conflict (name) do nothing;

            with saved_stock as (
                insert into stocks (
                    symbol,
                    exchange,
                    symbol_token,
                    trading_symbol,
                    created_at_utc
                )
                values (
                    @Symbol,
                    @Exchange,
                    @SymbolToken,
                    @TradingSymbol,
                    now()
                )
                on conflict (exchange, symbol_token) do update
                set symbol = excluded.symbol,
                    trading_symbol = excluded.trading_symbol,
                    updated_at_utc = now()
                returning id
            ),
            default_watchlist as (
                select id
                from watchlists
                where name = @WatchlistName
            )
            insert into watchlist_items (
                watchlist_id,
                stock_id,
                buy_target_price,
                sell_target_price,
                is_active,
                created_at_utc
            )
            select
                default_watchlist.id,
                saved_stock.id,
                @PurchaseRate,
                @SalesRate,
                true,
                now()
            from saved_stock
            cross join default_watchlist
            on conflict (watchlist_id, stock_id) do update
            set buy_target_price = excluded.buy_target_price,
                sell_target_price = excluded.sell_target_price,
                is_active = true,
                updated_at_utc = now()
            """,
            new
            {
                WatchlistName = watchlistName,
                stock.Symbol,
                stock.Exchange,
                stock.SymbolToken,
                stock.TradingSymbol,
                stock.PurchaseRate,
                stock.SalesRate
            });
    }

    public async Task DeleteAsync(string symbol, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(
            """
            update watchlist_items
            set is_active = false,
                updated_at_utc = now()
            from watchlists, stocks
            where watchlist_items.watchlist_id = watchlists.id
              and watchlist_items.stock_id = stocks.id
              and watchlists.name = @WatchlistName
              and stocks.symbol = @Symbol
            """,
            new { WatchlistName = DefaultWatchlistName, Symbol = symbol });
    }

    public async Task DeleteStockAsync(int watchlistId, int watchlistItemId, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(
            """
            update watchlist_items
            set is_active = false,
                updated_at_utc = now()
            where watchlist_id = @WatchlistId
              and id = @WatchlistItemId
            """,
            new { WatchlistId = watchlistId, WatchlistItemId = watchlistItemId });
    }
}
