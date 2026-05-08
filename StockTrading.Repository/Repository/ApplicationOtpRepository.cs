using Dapper;
using StockTrading.Data;
using StockTrading.Repository.IRepository;

namespace StockTrading.Repository.Repository;

public sealed class ApplicationOtpRepository(IDbConnectionFactory connectionFactory) : IApplicationOtpRepository
{
    public async Task CreateAsync(string userId, string otpHash, DateTime expiresAtUtc, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(
            """
            insert into user_otps (
                user_id,
                otp_hash,
                expires_at_utc,
                created_at_utc
            )
            values (
                @UserId,
                @OtpHash,
                @ExpiresAtUtc,
                @CreatedAtUtc
            )
            """,
            new
            {
                UserId = userId,
                OtpHash = otpHash,
                ExpiresAtUtc = expiresAtUtc,
                CreatedAtUtc = DateTime.UtcNow
            });
    }

    public async Task<long?> GetValidOtpIdAsync(string userId, string otpHash, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<long?>(
            """
            select id
            from user_otps
            where user_id = @UserId
              and otp_hash = @OtpHash
              and consumed_at_utc is null
              and expires_at_utc > @NowUtc
            order by created_at_utc desc
            limit 1
            """,
            new
            {
                UserId = userId,
                OtpHash = otpHash,
                NowUtc = nowUtc
            });
    }

    public async Task MarkConsumedAsync(long otpId, DateTime consumedAtUtc, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(
            """
            update user_otps
            set consumed_at_utc = @ConsumedAtUtc
            where id = @OtpId
            """,
            new
            {
                OtpId = otpId,
                ConsumedAtUtc = consumedAtUtc
            });
    }
}
