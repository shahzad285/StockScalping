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
                Id = ApplicationRoleNames.GetRoleId(roleName),
                Name = roleName,
                NormalizedName = NormalizeRoleName(roleName)
            };

            await connection.ExecuteAsync(
                """
                update roles
                set normalized_name = @NormalizedName
                where name = @Name;

                insert into roles (id, name, normalized_name, created_at_utc)
                select @Id, @Name, @NormalizedName, @CreatedAtUtc
                where not exists (
                    select 1
                    from roles
                    where id = @Id
                       or name = @Name
                       or normalized_name = @NormalizedName
                )
                """,
                role);
        }
    }

    public async Task AddUserToRoleAsync(int userId, string roleName, CancellationToken cancellationToken = default)
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

    public async Task<IReadOnlyList<string>> GetUserRolesAsync(int userId, CancellationToken cancellationToken = default)
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
