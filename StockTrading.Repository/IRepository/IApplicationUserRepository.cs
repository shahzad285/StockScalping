using StockTrading.Models;

namespace StockTrading.Repository.IRepository;

public interface IApplicationUserRepository
{
    Task<bool> AnyAsync(CancellationToken cancellationToken = default);
    Task<ApplicationUser?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<ApplicationUser?> GetByMobileNumberAsync(string mobileNumber, CancellationToken cancellationToken = default);
    Task AddAsync(ApplicationUser user, CancellationToken cancellationToken = default);
}
