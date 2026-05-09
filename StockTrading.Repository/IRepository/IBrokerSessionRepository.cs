using StockTrading.Models;

namespace StockTrading.Repository.IRepository;

public interface IBrokerSessionRepository
{
    Task<BrokerSessionRecord?> GetActiveAsync(
        string brokerName,
        int? userId = null,
        CancellationToken cancellationToken = default);

    Task SaveAsync(BrokerSessionRecord session, CancellationToken cancellationToken = default);

    Task ClearAsync(
        string brokerName,
        int? userId = null,
        CancellationToken cancellationToken = default);
}
