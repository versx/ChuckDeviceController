namespace ChuckDeviceController.Extensions.Http.Caching
{
    using Microsoft.Extensions.Caching.Memory;

    public class EntityMemoryCache : MemoryCache
    {
        #region Variables

        private static readonly MemoryCacheOptions _defaultMemCacheOptions = new()
        {
            // Reference: https://github.com/dotnet/runtime/blob/main/src/libraries/Microsoft.Extensions.Caching.Memory/src/MemoryCacheOptions.cs
            //SizeLimit = 1024 * 1024, // 1,048,576 (1 Mb)
            ExpirationScanFrequency = TimeSpan.FromMinutes(5), // default: 1 minute
            CompactionPercentage = 0.05, // default: 0.05
        };
        private readonly MemoryCache _memCache;

        #endregion

        public IMemoryCache Cache => _memCache;

        public EntityMemoryCacheConfig Options { get; }

        public EntityMemoryCache(EntityMemoryCacheConfig config)
            : base(_defaultMemCacheOptions)
        {
            Options = config;

            var options = new MemoryCacheOptions
            {
                CompactionPercentage = config.CompactionPercentage,
                ExpirationScanFrequency = TimeSpan.FromMinutes(config.ExpirationScanFrequencyM),
                SizeLimit = config.SizeLimit,
            };
            _memCache = new MemoryCache(options ?? _defaultMemCacheOptions);
        }
    }
}