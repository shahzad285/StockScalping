namespace StockTrading.Repository.IRepository;

public interface IApplicationOtpRepository
{
    Task CreateAsync(string userId, string otpHash, DateTime expiresAtUtc, CancellationToken cancellationToken = default);
    Task<long?> GetValidOtpIdAsync(string userId, string otpHash, DateTime nowUtc, CancellationToken cancellationToken = default);
    Task MarkConsumedAsync(long otpId, DateTime consumedAtUtc, CancellationToken cancellationToken = default);
}
