using Dapper;
using StockTrading.Data;
using StockTrading.Models;
using StockTrading.Repository.IRepository;

namespace StockTrading.Repository.Repository;

public sealed class BrokerSessionRepository(IDbConnectionFactory connectionFactory) : IBrokerSessionRepository
{
    public async Task<BrokerSessionRecord?> GetActiveAsync(
        string brokerName,
        int? userId = null,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<BrokerSessionRecord>(
            """
            select
                id as Id,
                broker_name as BrokerName,
                user_id as UserId,
                access_token_encrypted as AccessTokenEncrypted,
                refresh_token_encrypted as RefreshTokenEncrypted,
                feed_token_encrypted as FeedTokenEncrypted,
                access_token_expires_at_utc as AccessTokenExpiresAtUtc,
                refresh_token_expires_at_utc as RefreshTokenExpiresAtUtc,
                raw_data_json as RawDataJson,
                is_active as IsActive,
                created_at_utc as CreatedAtUtc,
                updated_at_utc as UpdatedAtUtc
            from broker_sessions
            where broker_name = @BrokerName
              and ((@UserId is null and user_id is null) or user_id = @UserId)
              and is_active = true
            order by updated_at_utc desc nulls last, created_at_utc desc
            limit 1
            """,
            new { BrokerName = brokerName, UserId = userId });
    }

    public async Task SaveAsync(BrokerSessionRecord session, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(
            """
            with updated as (
                update broker_sessions
                set access_token_encrypted = @AccessTokenEncrypted,
                    refresh_token_encrypted = @RefreshTokenEncrypted,
                    feed_token_encrypted = @FeedTokenEncrypted,
                    access_token_expires_at_utc = @AccessTokenExpiresAtUtc,
                    refresh_token_expires_at_utc = @RefreshTokenExpiresAtUtc,
                    raw_data_json = @RawDataJson,
                    is_active = true,
                    updated_at_utc = now()
                where broker_name = @BrokerName
                  and ((@UserId is null and user_id is null) or user_id = @UserId)
                returning id
            )
            insert into broker_sessions (
                broker_name,
                user_id,
                access_token_encrypted,
                refresh_token_encrypted,
                feed_token_encrypted,
                access_token_expires_at_utc,
                refresh_token_expires_at_utc,
                raw_data_json,
                is_active,
                created_at_utc
            )
            select
                @BrokerName,
                @UserId,
                @AccessTokenEncrypted,
                @RefreshTokenEncrypted,
                @FeedTokenEncrypted,
                @AccessTokenExpiresAtUtc,
                @RefreshTokenExpiresAtUtc,
                @RawDataJson,
                true,
                @CreatedAtUtc
            where not exists (select 1 from updated)
              and not exists (
                  select 1
                  from broker_sessions
                  where broker_name = @BrokerName
                    and ((@UserId is null and user_id is null) or user_id = @UserId)
              )
            """,
            session);
    }

    public async Task ClearAsync(
        string brokerName,
        int? userId = null,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(
            """
            update broker_sessions
            set is_active = false,
                updated_at_utc = now()
            where broker_name = @BrokerName
              and ((@UserId is null and user_id is null) or user_id = @UserId)
            """,
            new { BrokerName = brokerName, UserId = userId });
    }
}
