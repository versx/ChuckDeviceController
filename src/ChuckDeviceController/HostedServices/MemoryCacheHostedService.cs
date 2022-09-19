namespace ChuckDeviceController.HostedServices
{
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Options;

    using ChuckDeviceController.Common.Cache;
    using ChuckDeviceController.Data.Cache;
    using ChuckDeviceController.Data.Entities;

    public class MemoryCacheHostedService : BackgroundService, IMemoryCacheHostedService
    {
        private const ushort ExpiryLimitM = 60; // minutes

        #region Variables

        private static readonly ILogger<IMemoryCacheHostedService> _logger =
            new Logger<IMemoryCacheHostedService>(LoggerFactory.Create(x => x.AddConsole()));
        private readonly IMemoryCache _pokemonCache;
        private readonly IMemoryCache _gymCache;
        private readonly IMemoryCache _pokestopCache;
        private readonly IMemoryCache _incidentCache;
        private readonly IMemoryCache _s2CellCache;
        private readonly IMemoryCache _spawnpointCache;
        private readonly IMemoryCache _weatherCache;
        private readonly MemoryCacheOptions _options;

        #endregion

        public MemoryCacheHostedService(IOptions<MemoryCacheOptions> options)
        {
            _options = options.Value;

            _pokemonCache = new EntityMemoryCache(_options);
            _gymCache = new EntityMemoryCache(_options);
            _pokestopCache = new EntityMemoryCache(_options);
            _incidentCache = new EntityMemoryCache(_options);
            _s2CellCache = new EntityMemoryCache(_options);
            _spawnpointCache = new EntityMemoryCache(_options);
            _weatherCache = new EntityMemoryCache(_options);
        }

        public TEntity? Get<TKey, TEntity>(TKey key)
        {
            var value = default(TEntity);
            var name = typeof(TEntity).Name;
            switch (name)
            {
                case nameof(Pokemon):
                    value = _pokemonCache.Get<TEntity>(key);
                    break;
                case nameof(Gym):
                    value = _gymCache.Get<TEntity>(key);
                    break;
                case nameof(Pokestop):
                    value = _pokestopCache.Get<TEntity>(key);
                    break;
                case nameof(Incident):
                    value = _incidentCache.Get<TEntity>(key);
                    break;
                case nameof(Cell):
                    value = _s2CellCache.Get<TEntity>(key);
                    break;
                case nameof(Spawnpoint):
                    value = _spawnpointCache.Get<TEntity>(key);
                    break;
                case nameof(Weather):
                    value = _weatherCache.Get<TEntity>(key);
                    break;
            }
            return value;
        }

        public void Set<TKey, TEntity>(TKey key, TEntity obj, TimeSpan? expiry = null)
        {
            var defaultExpiry = TimeSpan.FromMinutes(ExpiryLimitM); // TODO: Make 'MemoryCacheHostedService.ExpiryLimitM' configurable
            var name = typeof(TEntity).Name;
            switch (name)
            {
                case nameof(Pokemon):
                    _pokemonCache.Set(key, obj, expiry ?? defaultExpiry);
                    break;
                case nameof(Gym):
                    _gymCache.Set(key, obj, expiry ?? defaultExpiry);
                    break;
                case nameof(Pokestop):
                    _pokestopCache.Set(key, obj, expiry ?? defaultExpiry);
                    break;
                case nameof(Incident):
                    _incidentCache.Set(key, obj, expiry ?? defaultExpiry);
                    break;
                case nameof(Cell):
                    _s2CellCache.Set(key, obj, expiry ?? defaultExpiry);
                    break;
                case nameof(Spawnpoint):
                    _spawnpointCache.Set(key, obj, expiry ?? defaultExpiry);
                    break;
                case nameof(Weather):
                    _weatherCache.Set(key, obj, expiry ?? defaultExpiry);
                    break;
            }
        }

        public void Unset<TKey, TEntity>(TKey key)
        {
            var name = typeof(TEntity).Name;
            switch (name)
            {
                case nameof(Pokemon):
                    _pokemonCache.Remove(key);
                    break;
                case nameof(Gym):
                    _gymCache.Remove(key);
                    break;
                case nameof(Pokestop):
                    _pokestopCache.Remove(key);
                    break;
                case nameof(Incident):
                    _incidentCache.Remove(key);
                    break;
                case nameof(Cell):
                    _s2CellCache.Remove(key);
                    break;
                case nameof(Spawnpoint):
                    _spawnpointCache.Remove(key);
                    break;
                case nameof(Weather):
                    _weatherCache.Remove(key);
                    break;
            }
        }

        public bool IsSet<TKey, TEntity>(TKey key)
        {
            var value = false;
            var name = typeof(TEntity).Name;
            switch (name)
            {
                case nameof(Pokemon):
                    value = _pokemonCache.TryGetValue<TEntity>(key, out var _);
                    break;
                case nameof(Gym):
                    value = _gymCache.TryGetValue<TEntity>(key, out var _);
                    break;
                case nameof(Pokestop):
                    value = _pokestopCache.TryGetValue<TEntity>(key, out var _);
                    break;
                case nameof(Incident):
                    value = _incidentCache.TryGetValue<TEntity>(key, out var _);
                    break;
                case nameof(Cell):
                    value = _s2CellCache.TryGetValue<TEntity>(key, out var _);
                    break;
                case nameof(Spawnpoint):
                    value = _spawnpointCache.TryGetValue<TEntity>(key, out var _);
                    break;
                case nameof(Weather):
                    value = _weatherCache.TryGetValue<TEntity>(key, out var _);
                    break;
            }
            return value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.CompletedTask;
        }
    }

    public class GenericMemoryCacheHostedService : BackgroundService, IMemoryCacheHostedService
    {
        private const ushort ExpiryLimitM = 60; // minutes

        #region Variables

        private static readonly ILogger<IMemoryCacheHostedService> _logger =
            new Logger<IMemoryCacheHostedService>(LoggerFactory.Create(x => x.AddConsole()));
        private readonly Dictionary<string, IMemoryCache> _memCache;
        private readonly MemoryCacheOptions _options;

        #endregion

        public GenericMemoryCacheHostedService(IOptions<MemoryCacheOptions> options, IEnumerable<string> cacheTypeNames)
        {
            _options = options.Value;
            _memCache = new Dictionary<string, IMemoryCache>();

            LoadCaches(cacheTypeNames);
        }

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
            var defaultExpiry = TimeSpan.FromMinutes(ExpiryLimitM); // TODO: Make 'MemoryCacheHostedService.ExpiryLimitM' configurable
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

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.CompletedTask;
        }

        private void LoadCaches(IEnumerable<string> typeNames)
        {
            foreach (var typeName in typeNames)
            {
                if (!_memCache.ContainsKey(typeName))
                {
                    _memCache.Add(typeName, new EntityMemoryCache(_options));
                }
            }
        }
    }
}