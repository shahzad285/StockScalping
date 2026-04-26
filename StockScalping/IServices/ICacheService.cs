namespace StockScalping.IServices;

public interface ICacheService
{
    /// <summary>
    /// Sets a value in cache
    /// </summary>
    /// <param name="key">Cache key</param>
    /// <param name="value">Value to store</param>
    void SetValue(string key, string value);

    /// <summary>
    /// Gets a value from cache
    /// </summary>
    /// <param name="key">Cache key</param>
    /// <returns>Cached value or null if not found</returns>
    string GetValue(string key);

    /// <summary>
    /// Removes a value from cache
    /// </summary>
    /// <param name="key">Cache key</param>
    void RemoveValue(string key);

    /// <summary>
    /// Checks if a key exists in cache
    /// </summary>
    /// <param name="key">Cache key</param>
    /// <returns>True if key exists</returns>
    bool HasKey(string key);

    /// <summary>
    /// Gets all cached keys (for debugging)
    /// </summary>
    /// <returns>List of all cache keys</returns>
    List<string> GetAllKeys();

    /// <summary>
    /// Clears all cache (for debugging/reset)
    /// </summary>
    void ClearAll();
}
