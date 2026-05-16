using Dapper;
using StockTrading.Common.DTOs;
using StockTrading.Data;
using StockTrading.Repository.IRepository;

namespace StockTrading.Repository.Repository;

public sealed class MarketJobDecisionRepository(IDbConnectionFactory connectionFactory) : IMarketJobDecisionRepository
{
    public async Task UpsertAsync(
        MarketJobDecisionResult decision,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(
            """
            insert into market_job_decisions (
                decision_date,
                exchange,
                job_name,
                is_trading_day,
                market_open_time,
                market_close_time,
                jobs_enabled,
                decision_reason,
                decided_at_utc,
                jobs_disabled_until_utc,
                created_at_utc
            )
            values (
                @DecisionDate,
                @Exchange,
                @JobName,
                @IsTradingDay,
                @MarketOpenTime,
                @MarketCloseTime,
                @JobsEnabled,
                @DecisionReason,
                now(),
                @JobsDisabledUntilUtc,
                now()
            )
            on conflict (decision_date, exchange, job_name) do update
            set is_trading_day = excluded.is_trading_day,
                market_open_time = excluded.market_open_time,
                market_close_time = excluded.market_close_time,
                jobs_enabled = excluded.jobs_enabled,
                decision_reason = excluded.decision_reason,
                decided_at_utc = now(),
                jobs_disabled_until_utc = excluded.jobs_disabled_until_utc,
                updated_at_utc = now()
            """,
            new
            {
                DecisionDate = decision.DecisionDate.ToDateTime(TimeOnly.MinValue),
                decision.Exchange,
                decision.JobName,
                decision.IsTradingDay,
                MarketOpenTime = decision.MarketOpenTime.ToTimeSpan(),
                MarketCloseTime = decision.MarketCloseTime.ToTimeSpan(),
                decision.JobsEnabled,
                decision.DecisionReason,
                decision.JobsDisabledUntilUtc
            });
    }
}
