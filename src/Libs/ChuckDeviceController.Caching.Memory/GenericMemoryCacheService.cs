namespace ChuckDeviceController.Caching.Memory;

using System.Collections.Concurrent;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public class GenericMemoryCacheService : IMemoryCacheService
{
    #region Variables

    private readonly ILogger<IMemoryCacheService> _logger;
    private readonly ConcurrentDictionary<string, IMemoryCache> _memCache = new();
    private readonly EntityMemoryCacheConfig _options;
    private readonly TimeSpan _defaultEntityExpiry;

    #endregion

    #region Constructor

    public GenericMemoryCacheService(
        ILogger<IMemoryCacheService> logger,
        IOptions<EntityMemoryCacheConfig>? options)
    {
        _logger = logger;
        _options = options?.Value ?? new EntityMemoryCacheConfig();
        _defaultEntityExpiry = TimeSpan.FromMinutes(_options.EntityExpiryLimitM);

        LoadCaches(_options.EntityTypeNames);
    }

    #endregion

    #region Public Methods

    public TEntity? Get<TKey, TEntity>(TKey key)
        where TKey : notnull
    {
        var value = default(TEntity);
        var name = typeof(TEntity).Name;

        if (_memCache.TryGetValue(name, out var memCache))
        {
            value = memCache.Get<TEntity>(key);
        }
        return value;
    }

    public void Set<TKey, TEntity>(TKey key, TEntity obj, TimeSpan? expiry = null)
        where TKey : notnull
        where TEntity : class
    {
        var name = typeof(TEntity).Name;

        if (_memCache.TryGetValue(name, out var memCache))
        {
            memCache.Set(key, obj, new MemoryCacheEntryOptions
            {
                Size = 1,
                AbsoluteExpirationRelativeToNow = expiry ?? _defaultEntityExpiry,
            });
            //memCache.Set(key, obj, expiry ?? _defaultEntityExpiry);
        }
    }

    public void Unset<TKey, TEntity>(TKey key)
        where TKey : notnull
    {
        var name = typeof(TEntity).Name;

        if (_memCache.TryGetValue(name, out var memCache))
        {
            memCache.Remove(key);
        }
    }

    public bool IsSet<TKey, TEntity>(TKey key)
        where TKey : notnull
    {
        var value = false;
        var name = typeof(TEntity).Name;

        if (_memCache.TryGetValue(name, out var memCache))
        {
            value = memCache.TryGetValue<TEntity>(key, out var _);
        }
        return value;
    }

    public bool Clear<TEntity>()
    {
        var name = typeof(TEntity).Name;
        if (_memCache.TryGetValue(name, out IMemoryCache? value))
        {
            value?.Dispose();
            _memCache.Remove(name, out var _);

            var cache = new EntityMemoryCache(_options);
            _memCache.AddOrUpdate(name, cache, (key, oldValue) => cache);

            return true;
        }
        return false;
    }

    #endregion

    #region Private Methods

    private void LoadCaches(IEnumerable<string> typeNames)
    {
        _logger.LogInformation("Initializing {Count:N0} memory caches...", typeNames.Count());

        foreach (var typeName in typeNames)
        {
            if (!_memCache.ContainsKey(typeName))
            {
                _memCache.AddOrUpdate(typeName, new EntityMemoryCache(_options), (key, oldValue) => oldValue);
            }
        }
    }

    #endregion
}