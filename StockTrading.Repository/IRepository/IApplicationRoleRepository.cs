using StockTrading.Models;

namespace StockTrading.Repository.IRepository;

public interface IApplicationRoleRepository
{
    Task EnsureRolesAsync(IEnumerable<string> roleNames, CancellationToken cancellationToken = default);
    Task AddUserToRoleAsync(string userId, string roleName, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetUserRolesAsync(string userId, CancellationToken cancellationToken = default);
}
