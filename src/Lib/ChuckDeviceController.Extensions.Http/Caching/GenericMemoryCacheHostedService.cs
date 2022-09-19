namespace ChuckDeviceController.Extensions.Http.Caching
{
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    public class GenericMemoryCacheHostedService : BackgroundService, IMemoryCacheHostedService
    {
        #region Variables

        private readonly ILogger<IMemoryCacheHostedService> _logger;
        private readonly Dictionary<string, IMemoryCache> _memCache = new();
        private readonly EntityMemoryCacheConfig _options;

        #endregion

        #region Constructor

        public GenericMemoryCacheHostedService(
            ILogger<IMemoryCacheHostedService> logger,
            IOptions<EntityMemoryCacheConfig>? options)
        {
            _logger = logger;
            _options = options?.Value ?? new EntityMemoryCacheConfig();

            LoadCaches(_options.EntityNames);
        }

        #endregion

        #region Public Methods

        public TEntity? Get<TKey, TEntity>(TKey key)
        {
            var value = default(TEntity);
            var name = typeof(TEntity).Name;

            if (_memCache.ContainsKey(name))
            {
                var memCache = _memCache[name];
                value = memCache.Get<TEntity>(key);
            }
            return value;
        }

        public void Set<TKey, TEntity>(TKey key, TEntity obj, TimeSpan? expiry = null)
        {
            var defaultExpiry = TimeSpan.FromMinutes(_options.EntityExpiryLimitM);
            var name = typeof(TEntity).Name;

            if (_memCache.ContainsKey(name))
            {
                var memCache = _memCache[name];
                memCache.Set(key, obj, expiry ?? defaultExpiry);
            }
        }

        public void Unset<TKey, TEntity>(TKey key)
        {
            var name = typeof(TEntity).Name;

            if (_memCache.ContainsKey(name))
            {
                var memCache = _memCache[name];
                memCache.Remove(key);
            }
        }

        public bool IsSet<TKey, TEntity>(TKey key)
        {
            var value = false;
            var name = typeof(TEntity).Name;

            if (_memCache.ContainsKey(name))
            {
                var memCache = _memCache[name];
                value = memCache.TryGetValue<TEntity>(key, out var _);
            }
            return value;
        }

        #endregion

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.CompletedTask;
        }

        #region Private Methods

        private void LoadCaches(IReadOnlyList<string> typeNames)
        {
            _logger.LogInformation($"Initializing {typeNames.Count:N0} memory caches...");

            foreach (var typeName in typeNames)
            {
                if (!_memCache.ContainsKey(typeName))
                {
                    _memCache.Add(typeName, new EntityMemoryCache(_options));
                }
            }
        }

        #endregion
    }
}