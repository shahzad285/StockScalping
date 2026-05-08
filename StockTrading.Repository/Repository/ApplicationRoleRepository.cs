using Dapper;
using StockTrading.Data;
using StockTrading.Models;
using StockTrading.Repository.IRepository;

namespace StockTrading.Repository.Repository;

public sealed class ApplicationRoleRepository(IDbConnectionFactory connectionFactory) : IApplicationRoleRepository
{
    public async Task EnsureRolesAsync(IEnumerable<string> roleNames, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        foreach (var roleName in roleNames)
        {
            var role = new ApplicationRole
            {
                Name = roleName,
                NormalizedName = NormalizeRoleName(roleName)
            };

            await connection.ExecuteAsync(
                """
                insert into roles (id, name, normalized_name, created_at_utc)
                values (@Id, @Name, @NormalizedName, @CreatedAtUtc)
                on conflict (normalized_name) do nothing
                """,
                role);
        }
    }

    public async Task AddUserToRoleAsync(string userId, string roleName, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(
            """
            insert into user_roles (user_id, role_id)
            select @UserId, id
            from roles
            where normalized_name = @NormalizedRoleName
            on conflict do nothing
            """,
            new
            {
                UserId = userId,
                NormalizedRoleName = NormalizeRoleName(roleName)
            });
    }

    public async Task<IReadOnlyList<string>> GetUserRolesAsync(string userId, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var roles = await connection.QueryAsync<string>(
            """
            select r.name
            from roles r
            inner join user_roles ur on ur.role_id = r.id
            where ur.user_id = @UserId
            order by r.name
            """,
            new { UserId = userId });

        return roles.ToArray();
    }

    private static string NormalizeRoleName(string roleName)
    {
        return roleName.Trim().ToUpperInvariant();
    }
}
