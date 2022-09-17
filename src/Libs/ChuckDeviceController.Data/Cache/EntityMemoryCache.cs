namespace ChuckDeviceController.Data.Cache
{
    using Microsoft.Extensions.Caching.Memory;

    public class EntityMemoryCache : MemoryCache
    {
        #region Variables

        private static readonly MemoryCacheOptions _defaultMemCacheOptions = new()
        {
            //SizeLimit = 2 * 10240,
            ExpirationScanFrequency = TimeSpan.FromSeconds(15),
            CompactionPercentage = 0.50,
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