namespace StockTrading.IServices;

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
    string? GetValue(string key);

}
