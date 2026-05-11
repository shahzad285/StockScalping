using Dapper;
using StockTrading.Common.DTOs;
using StockTrading.Data;
using StockTrading.Models;
using StockTrading.Repository.IRepository;

namespace StockTrading.Repository.Repository;

public sealed class StockProfileRepository(IDbConnectionFactory connectionFactory) : IStockProfileRepository
{
    public async Task<IReadOnlyList<Stock>> GetStocksNeedingProfileAsync(
        int limit,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var stocks = await connection.QueryAsync<Stock>(
            """
            select distinct
                stocks.id as Id,
                stocks.symbol as Symbol,
                stocks.exchange as Exchange,
                stocks.symbol_token as SymbolToken,
                stocks.trading_symbol as TradingSymbol,
                stocks.name as Name,
                stocks.created_at_utc as CreatedAtUtc,
                stocks.updated_at_utc as UpdatedAtUtc
            from stocks
            left join stock_profiles
              on stock_profiles.stock_id = stocks.id
            where (
                exists (
                    select 1
                    from watchlist
                    where watchlist.stock_id = stocks.id
                      and watchlist.is_active = true
                )
                or exists (
                    select 1
                    from trade_plans
                    where trade_plans.stock_id = stocks.id
                      and trade_plans.is_active = true
                )
            )
              and (
                  stock_profiles.id is null
                  or nullif(trim(stock_profiles.sector), '') is null
                  or nullif(trim(stock_profiles.industry), '') is null
                  or stock_profiles.market_cap is null
                  or stock_profiles.pe_ratio is null
                  or stock_profiles.last_analyzed_at_utc is null
              )
            order by stocks.symbol
            limit @Limit
            """,
            new { Limit = Math.Max(1, limit) });

        return stocks.ToArray();
    }

    public async Task UpsertFundamentalsAsync(
        Stock stock,
        AlphaVantageCompanyOverview overview,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(
            """
            insert into stock_profiles (
                stock_id,
                asset_type,
                sector,
                industry,
                description,
                dividend_yield,
                debt_to_equity,
                pe_ratio,
                market_cap,
                last_analyzed_at_utc,
                created_at_utc
            )
            values (
                @StockId,
                @AssetType,
                @Sector,
                @Industry,
                @Description,
                @DividendYield,
                @DebtToEquity,
                @PeRatio,
                @MarketCap,
                now(),
                now()
            )
            on conflict (stock_id) do update
            set sector = coalesce(excluded.sector, stock_profiles.sector),
                industry = coalesce(excluded.industry, stock_profiles.industry),
                description = coalesce(excluded.description, stock_profiles.description),
                dividend_yield = coalesce(excluded.dividend_yield, stock_profiles.dividend_yield),
                debt_to_equity = coalesce(excluded.debt_to_equity, stock_profiles.debt_to_equity),
                pe_ratio = coalesce(excluded.pe_ratio, stock_profiles.pe_ratio),
                market_cap = coalesce(excluded.market_cap, stock_profiles.market_cap),
                last_analyzed_at_utc = now(),
                updated_at_utc = now()
            """,
            new
            {
                StockId = stock.Id,
                AssetType = "Equity",
                Sector = ToDbValue(overview.Sector),
                Industry = ToDbValue(overview.Industry),
                Description = ToDbValue(overview.Description),
                overview.DividendYield,
                overview.DebtToEquity,
                PeRatio = overview.PERatio,
                MarketCap = overview.MarketCapitalization
            });
    }

    private static string? ToDbValue(string value)
    {
        return string.IsNullOrWhiteSpace(value) || value.Equals("None", StringComparison.OrdinalIgnoreCase)
            ? null
            : value.Trim();
    }
}
