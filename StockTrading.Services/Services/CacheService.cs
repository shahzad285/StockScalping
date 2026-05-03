using StockTrading.IServices;

namespace StockTrading.Services;

public class CacheService : ICacheService
{
    private readonly Dictionary<string, string> _cache = new Dictionary<string, string>();
    private readonly object _lockObject = new object();

    /// <summary>
    /// Sets a value in cache
    /// </summary>
    /// <param name="key">Cache key</param>
    /// <param name="value">Value to store</param>
    public void SetValue(string key, string value)
    {
        lock (_lockObject)
        {
            _cache[key] = value;
        }
    }

    /// <summary>
    /// Gets a value from cache
    /// </summary>
    /// <param name="key">Cache key</param>
    /// <returns>Cached value or null if not found</returns>
    public string? GetValue(string key)
    {
        lock (_lockObject)
        {
            return _cache.TryGetValue(key, out var value) ? value : null;
        }
    }

}
