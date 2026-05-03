using Microsoft.Extensions.Hosting;

namespace StockTrading.IServices;

public interface IScalpingService : IHostedService
{
    /// <summary>
    /// Scalping service for automated stock trading
    /// </summary>
}
