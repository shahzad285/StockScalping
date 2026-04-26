using StockScalping.IServices;

namespace StockScalping.Services;

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
    public string GetValue(string key)
    {
        lock (_lockObject)
        {
            return _cache.TryGetValue(key, out var value) ? value : null;
        }
    }

    /// <summary>
    /// Removes a value from cache
    /// </summary>
    /// <param name="key">Cache key</param>
    public void RemoveValue(string key)
    {
        lock (_lockObject)
        {
            _cache.Remove(key);
        }
    }

    /// <summary>
    /// Checks if a key exists in cache
    /// </summary>
    /// <param name="key">Cache key</param>
    /// <returns>True if key exists</returns>
    public bool HasKey(string key)
    {
        lock (_lockObject)
        {
            return _cache.ContainsKey(key);
        }
    }

    /// <summary>
    /// Gets all cached keys (for debugging)
    /// </summary>
    /// <returns>List of all cache keys</returns>
    public List<string> GetAllKeys()
    {
        lock (_lockObject)
        {
            return _cache.Keys.ToList();
        }
    }

    /// <summary>
    /// Clears all cache (for debugging/reset)
    /// </summary>
    public void ClearAll()
    {
        lock (_lockObject)
        {
            _cache.Clear();
            System.Console.WriteLine("Cache cleared");
        }
    }
}
