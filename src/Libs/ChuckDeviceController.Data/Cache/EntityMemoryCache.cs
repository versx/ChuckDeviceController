namespace ChuckDeviceController.Data.Cache
{
    using Microsoft.Extensions.Caching.Memory;

    public class EntityMemoryCache : MemoryCache
    {
        #region Variables

        private static readonly MemoryCacheOptions _defaultMemCacheOptions = new()
        {
            // Reference: https://github.com/dotnet/runtime/blob/main/src/libraries/Microsoft.Extensions.Caching.Memory/src/MemoryCacheOptions.cs
            //SizeLimit = 2 * 10240,
            ExpirationScanFrequency = TimeSpan.FromSeconds(15), // default: 1 minute
            CompactionPercentage = 0.50, // default: 0.05
        };
        private readonly MemoryCache _memCache;

        #endregion

        public IMemoryCache Cache => _memCache;

        public EntityMemoryCache(MemoryCacheOptions? options = null)
            : base(_defaultMemCacheOptions)
        {
            _memCache = new MemoryCache(options ?? _defaultMemCacheOptions);
        }
    }
}