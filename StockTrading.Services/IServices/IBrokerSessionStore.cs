using StockTrading.Models;

namespace StockTrading.IServices;

public interface IBrokerSessionStore
{
    Task<BrokerSession?> GetAsync(
        string brokerName,
        int? userId = null,
        CancellationToken cancellationToken = default);

    Task SaveAsync(BrokerSession session, CancellationToken cancellationToken = default);

    Task ClearAsync(
        string brokerName,
        int? userId = null,
        CancellationToken cancellationToken = default);
}
