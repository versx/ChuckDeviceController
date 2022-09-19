namespace ChuckDeviceConfigurator.HostedServices
{
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Options;

    using ChuckDeviceController.Common.Cache;
    using ChuckDeviceController.Data.Cache;
    using ChuckDeviceController.Data.Entities;

    public class MemoryCacheHostedService : BackgroundService, IMemoryCacheHostedService
    {
        private const ushort ExpiryLimitM = 15; // minutes

        #region Variables

        private static readonly ILogger<IMemoryCacheHostedService> _logger =
            new Logger<IMemoryCacheHostedService>(LoggerFactory.Create(x => x.AddConsole()));
        private readonly IMemoryCache _deviceCache;
        private readonly IMemoryCache _accountCache;
        private readonly MemoryCacheOptions _options;

        #endregion

        public MemoryCacheHostedService(IOptions<MemoryCacheOptions> options)
        {
            _options = options.Value;

            _deviceCache = new EntityMemoryCache(_options);
            _accountCache = new EntityMemoryCache(_options);
        }

        public TEntity? Get<TKey, TEntity>(TKey key)
        {
            var value = default(TEntity);
            var name = typeof(TEntity).Name;
            switch (name)
            {
                case nameof(Device):
                    value = _deviceCache.Get<TEntity>(key);
                    break;
                case nameof(Account):
                    value = _accountCache.Get<TEntity>(key);
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
                case nameof(Device):
                    _deviceCache.Set(key, obj, expiry ?? defaultExpiry);
                    break;
                case nameof(Account):
                    _accountCache.Set(key, obj, expiry ?? defaultExpiry);
                    break;
            }
        }

        public void Unset<TKey, TEntity>(TKey key)
        {
            var name = typeof(TEntity).Name;
            switch (name)
            {
                case nameof(Device):
                    _deviceCache.Remove(key);
                    break;
                case nameof(Account):
                    _accountCache.Remove(key);
                    break;
            }
        }

        public bool IsSet<TKey, TEntity>(TKey key)
        {
            var value = false;
            var name = typeof(TEntity).Name;
            switch (name)
            {
                case nameof(Device):
                    value = _deviceCache.TryGetValue<TEntity>(key, out var _);
                    break;
                case nameof(Account):
                    value = _accountCache.TryGetValue<TEntity>(key, out var _);
                    break;
            }
            return value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.CompletedTask;
        }
    }
}