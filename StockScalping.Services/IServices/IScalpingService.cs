using Microsoft.Extensions.Hosting;

namespace StockScalping.IServices;

public interface IScalpingService : IHostedService
{
    /// <summary>
    /// Scalping service for automated stock trading
    /// </summary>
}
