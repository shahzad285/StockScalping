using StockTrading.Models;

namespace StockTrading.Repository.IRepository;

public interface IApplicationRoleRepository
{
    Task EnsureRolesAsync(IEnumerable<string> roleNames, CancellationToken cancellationToken = default);
    Task AddUserToRoleAsync(int userId, string roleName, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetUserRolesAsync(int userId, CancellationToken cancellationToken = default);
}
