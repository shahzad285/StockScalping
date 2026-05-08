using Dapper;
using StockTrading.Data;
using StockTrading.Models;
using StockTrading.Repository.IRepository;

namespace StockTrading.Repository.Repository;

public sealed class ApplicationUserRepository(IDbConnectionFactory connectionFactory) : IApplicationUserRepository
{
    public async Task<bool> AnyAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<bool>("select exists(select 1 from users)");
    }

    public async Task<ApplicationUser?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<ApplicationUser>(
            """
            select
                id,
                mobile_number as MobileNumber,
                normalized_mobile_number as NormalizedMobileNumber,
                is_active as IsActive,
                created_at_utc as CreatedAtUtc
            from users
            where id = @Id
            """,
            new { Id = id });
    }

    public async Task<ApplicationUser?> GetByMobileNumberAsync(string mobileNumber, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<ApplicationUser>(
            """
            select
                id,
                mobile_number as MobileNumber,
                normalized_mobile_number as NormalizedMobileNumber,
                is_active as IsActive,
                created_at_utc as CreatedAtUtc
            from users
            where normalized_mobile_number = @NormalizedMobileNumber
            """,
            new { NormalizedMobileNumber = NormalizeMobileNumber(mobileNumber) });
    }

    public async Task AddAsync(ApplicationUser user, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        user.NormalizedMobileNumber = NormalizeMobileNumber(user.MobileNumber);

        await connection.ExecuteAsync(
            """
            insert into users (
                id,
                mobile_number,
                normalized_mobile_number,
                is_active,
                created_at_utc
            )
            values (
                @Id,
                @MobileNumber,
                @NormalizedMobileNumber,
                @IsActive,
                @CreatedAtUtc
            )
            """,
            user);
    }

    private static string NormalizeMobileNumber(string mobileNumber)
    {
        return mobileNumber.Trim();
    }
}
