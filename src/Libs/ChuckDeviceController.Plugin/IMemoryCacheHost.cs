namespace ChuckDeviceController.Plugin;

/// <summary>
/// In memory cache host handler.
/// </summary>
public interface IMemoryCacheHost
{
    /// <summary>
    /// Checks whether a key exists in the cache.
    /// </summary>
    /// <param name="key">Key to check if exists.</param>
    /// <returns>Returns <c>true</c> if the key exists, otherwise <c>false</c>.</returns>
    bool IsSet(string key);

    /// <summary>
    /// Trys to retrieve a value by key from the cache.
    /// </summary>
    /// <typeparam name="T">Type of value.</typeparam>
    /// <param name="key">Key to check.</param>
    /// <param name="value">Value returned from the cache.</param>
    /// <returns>Returns <c>true</c> if the key exists, otherwise <c>false</c>.</returns>
    bool TryGetValue<T>(string key, out T? value);

    /// <summary>
    /// Retrieve a value by key from the cache.
    /// </summary>
    /// <typeparam name="T">Type of value.</typeparam>
    /// <param name="key">Key to check.</param>
    /// <returns>Returns a value from the cache, otherwise <c>null</c>.</returns>
    T? GetValue<T>(string key);

    /// <summary>
    /// Caches a value by key with a set expiration time.
    /// </summary>
    /// <typeparam name="T">Type of value.</typeparam>
    /// <param name="key">Key to set.</param>
    /// <param name="value">Value to cache.</param>
    /// <param name="expiryS">Expiration time in seconds.</param>
    void SetValue<T>(string key, T value, ushort expiryS);

    /// <summary>
    /// Remove a entry from the cache by key.
    /// </summary>
    /// <param name="key">Key to remove from the cache.</param>
    void Remove(string key);

    /// <summary>
    /// Clears all cached entries.
    /// </summary>
    void Clear();
}