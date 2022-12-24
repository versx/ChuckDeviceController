namespace ChuckDeviceController.Caching.Memory;

using Microsoft.Extensions.Caching.Memory;

// https://briancaos.wordpress.com/2021/02/16/simple-c-memorycache-implementation-understand-the-sizelimit-property/
public class MemoryCacheRepository : IMemoryCacheRepository
{
    #region Constants

    // Hold a maximum of 1024 cache entries
    private const int SizeLimit = 1024;

    // Expire cache entry after 15 minutes
    private const int AbsoluteExpirationS = 90;

    #endregion

    #region Variables

    private readonly MemoryCache _cache;

    #endregion

    #region Constructors

    public MemoryCacheRepository()
    {
        _cache = new MemoryCache(new MemoryCacheOptions
        {
            SizeLimit = SizeLimit,
        });
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Returns a value determining whether or not a cached
    /// entry exists by key.
    /// </summary>
    /// <param name="key">Cached entry key to check if set.</param>
    /// <returns>Returns true if the key exists.</returns>
    public bool IsSet(string key)
    {
        return _cache.TryGetValue(key, out var _);
    }

    /// <summary>
    /// Try getting a value from the cache.
    /// </summary>
    /// <typeparam name="T">Value type of cache entry.</typeparam>
    /// <param name="key">Cached entry key to set.</param>
    /// <param name="value"></param>
    /// <returns></returns>
    public bool TryGetValue<T>(string key, out T? value)
    {
        value = default;

        if (_cache.TryGetValue(key, out T? result))
        {
            value = result;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Retrieve a value from the cache.
    /// </summary>
    /// <typeparam name="T">Value type of cache entry.</typeparam>
    /// <param name="key">Cached entry key to set.</param>
    /// <returns></returns>
    public T? GetValue<T>(string key)
    {
        var value = _cache.Get<T?>(key);
        return value;

    }

    /// <summary>
    /// Adding a value to the cache. All entries
    /// have size = 1 and will expire after 15 minutes
    /// </summary>
    /// <typeparam name="T">Value type of cache entry.</typeparam>
    /// <param name="key">Cached entry key to set.</param>
    /// <param name="value">Value to cache for key.</param>
    /// <param name="expiryS">Time in seconds before cache entry expires.</param>
    public void SetValue<T>(string key, T value, ushort expiryS = AbsoluteExpirationS)
    {
        _cache.Set(key, value, new MemoryCacheEntryOptions()
          .SetSize(1)
          .SetAbsoluteExpiration(TimeSpan.FromSeconds(expiryS))
        );
    }

    /// <summary>
    /// Remove entry from cache.
    /// </summary>
    /// <param name="key">Cached entry key to remove.</param>
    public void Remove(string key)
    {
        _cache.Remove(key);
    }

    /// <summary>
    /// Clear all cached entries.
    /// </summary>
    public void Clear()
    {
        _cache.Clear();
    }

    #endregion
}
