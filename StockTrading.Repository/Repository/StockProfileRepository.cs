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
                  or (
                      nullif(trim(stock_profiles.sector), '') is null
                      and nullif(trim(stock_profiles.industry), '') is null
                  )
                  or stock_profiles.market_cap is null
                  or stock_profiles.last_analyzed_at_utc is null
              )
            order by stocks.symbol
            limit @Limit
            """,
            new { Limit = Math.Max(1, limit) });

        return stocks.ToArray();
    }

    private static string? ToDbValue(string value)
    {
        return string.IsNullOrWhiteSpace(value) || value.Equals("None", StringComparison.OrdinalIgnoreCase)
            ? null
            : value.Trim();
    }

    public async Task UpsertFundamentalsAsync(
        Stock stock,
        TapetideCompanyProfile profile,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(
            """
            update stocks
            set name = coalesce(@Name, stocks.name),
                updated_at_utc = case when @Name is null then updated_at_utc else now() end
            where id = @StockId;

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
                Name = ToDbValue(profile.Name),
                AssetType = "Equity",
                Sector = ToDbValue(profile.Sector),
                Industry = ToDbValue(profile.Industry),
                Description = ToDbValue(profile.Description),
                profile.DividendYield,
                profile.DebtToEquity,
                PeRatio = profile.PERatio,
                MarketCap = profile.MarketCapitalization
            });
    }

    public async Task UpsertFundamentalsAsync(
        Stock stock,
        YahooFinanceCompanyProfile profile,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(
            """
            update stocks
            set name = coalesce(@Name, stocks.name),
                updated_at_utc = case when @Name is null then updated_at_utc else now() end
            where id = @StockId;

            insert into stock_profiles (
                stock_id,
                asset_type,
                sector,
                industry,
                description,
                dividend_yield,
                debt_to_equity,
                pe_ratio,
                earnings_per_share,
                price_to_book,
                total_revenue,
                net_income,
                total_debt,
                total_cash,
                cash_flow,
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
                @EarningsPerShare,
                @PriceToBook,
                @TotalRevenue,
                @NetIncome,
                @TotalDebt,
                @TotalCash,
                @CashFlow,
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
                earnings_per_share = coalesce(excluded.earnings_per_share, stock_profiles.earnings_per_share),
                price_to_book = coalesce(excluded.price_to_book, stock_profiles.price_to_book),
                total_revenue = coalesce(excluded.total_revenue, stock_profiles.total_revenue),
                net_income = coalesce(excluded.net_income, stock_profiles.net_income),
                total_debt = coalesce(excluded.total_debt, stock_profiles.total_debt),
                total_cash = coalesce(excluded.total_cash, stock_profiles.total_cash),
                cash_flow = coalesce(excluded.cash_flow, stock_profiles.cash_flow),
                market_cap = coalesce(excluded.market_cap, stock_profiles.market_cap),
                last_analyzed_at_utc = now(),
                updated_at_utc = now()
            """,
            new
            {
                StockId = stock.Id,
                Name = ToDbValue(profile.Name),
                AssetType = "Equity",
                Sector = ToDbValue(profile.Sector),
                Industry = ToDbValue(profile.Industry),
                Description = ToDbValue(profile.Description),
                profile.DividendYield,
                profile.DebtToEquity,
                PeRatio = profile.PERatio,
                profile.EarningsPerShare,
                profile.PriceToBook,
                profile.TotalRevenue,
                profile.NetIncome,
                profile.TotalDebt,
                profile.TotalCash,
                profile.CashFlow,
                MarketCap = profile.MarketCapitalization
            });
    }

    public async Task UpsertFundamentalsAsync(
        Stock stock,
        NseIndiaEquityProfile profile,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(
            """
            update stocks
            set name = coalesce(@Name, stocks.name),
                updated_at_utc = case when @Name is null then updated_at_utc else now() end
            where id = @StockId;

            insert into stock_profiles (
                stock_id,
                asset_type,
                industry,
                last_analyzed_at_utc,
                created_at_utc
            )
            values (
                @StockId,
                @AssetType,
                @Industry,
                now(),
                now()
            )
            on conflict (stock_id) do update
            set industry = coalesce(excluded.industry, stock_profiles.industry),
                last_analyzed_at_utc = now(),
                updated_at_utc = now()
            """,
            new
            {
                StockId = stock.Id,
                Name = ToDbValue(profile.CompanyName),
                AssetType = "Equity",
                Industry = ToDbValue(profile.Industry)
            });
    }
}
