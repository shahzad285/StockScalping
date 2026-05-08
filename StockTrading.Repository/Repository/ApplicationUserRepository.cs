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

    public async Task<ApplicationUser?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<ApplicationUser>(
            """
            select
                id,
                name,
                email,
                normalized_email as NormalizedEmail,
                phone_number as PhoneNumber,
                normalized_phone_number as NormalizedPhoneNumber,
                is_active as IsActive,
                created_at_utc as CreatedAtUtc
            from users
            where id = @Id
            """,
            new { Id = id });
    }

    public async Task<ApplicationUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<ApplicationUser>(
            """
            select
                id,
                name,
                email,
                normalized_email as NormalizedEmail,
                phone_number as PhoneNumber,
                normalized_phone_number as NormalizedPhoneNumber,
                is_active as IsActive,
                created_at_utc as CreatedAtUtc
            from users
            where normalized_email = @NormalizedEmail
            """,
            new { NormalizedEmail = NormalizeEmail(email) });
    }

    public async Task<ApplicationUser?> GetByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<ApplicationUser>(
            """
            select
                id,
                name,
                email,
                normalized_email as NormalizedEmail,
                phone_number as PhoneNumber,
                normalized_phone_number as NormalizedPhoneNumber,
                is_active as IsActive,
                created_at_utc as CreatedAtUtc
            from users
            where normalized_phone_number = @NormalizedPhoneNumber
            """,
            new { NormalizedPhoneNumber = NormalizePhoneNumber(phoneNumber) });
    }

    public async Task AddAsync(ApplicationUser user, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        user.NormalizedEmail = NormalizeEmail(user.Email);
        user.NormalizedPhoneNumber = NormalizePhoneNumber(user.PhoneNumber);

        user.Id = await connection.ExecuteScalarAsync<int>(
            """
            insert into users (
                name,
                email,
                normalized_email,
                phone_number,
                normalized_phone_number,
                is_active,
                created_at_utc
            )
            values (
                @Name,
                @Email,
                @NormalizedEmail,
                @PhoneNumber,
                @NormalizedPhoneNumber,
                @IsActive,
                @CreatedAtUtc
            )
            returning id
            """,
            user);
    }

    private static string? NormalizeEmail(string? email)
    {
        return string.IsNullOrWhiteSpace(email)
            ? null
            : email.Trim().ToUpperInvariant();
    }

    private static string? NormalizePhoneNumber(string? phoneNumber)
    {
        return string.IsNullOrWhiteSpace(phoneNumber)
            ? null
            : phoneNumber.Trim();
    }
}
