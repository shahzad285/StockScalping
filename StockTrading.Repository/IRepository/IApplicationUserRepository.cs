using StockTrading.Models;

namespace StockTrading.Repository.IRepository;

public interface IApplicationUserRepository
{
    Task<bool> AnyAsync(CancellationToken cancellationToken = default);
    Task<ApplicationUser?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ApplicationUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<ApplicationUser?> GetByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken = default);
    Task AddAsync(ApplicationUser user, CancellationToken cancellationToken = default);
}
