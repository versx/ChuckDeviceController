﻿namespace ChuckDeviceController.Data.Cache
{
    using Microsoft.Extensions.Caching.Memory;

    public class EntityMemoryCache : MemoryCache
    {
        private const uint DefaultMaxSizeLimit = 1024 * 1024; // 1,048,576 (1 Mb)
        private const uint DefaultMaxExpiryScanInterval = 5; // minutes
        private const double DefaultCompactionPercentage = 0.05;

        #region Variables

        private static readonly MemoryCacheOptions _defaultMemCacheOptions = new()
        {
            // Reference: https://github.com/dotnet/runtime/blob/main/src/libraries/Microsoft.Extensions.Caching.Memory/src/MemoryCacheOptions.cs
            //SizeLimit = DefaultMaxSizeLimit,
            ExpirationScanFrequency = TimeSpan.FromMinutes(DefaultMaxExpiryScanInterval), // default: 1 minute
            CompactionPercentage = DefaultCompactionPercentage, // default: 0.05
        };
        private readonly MemoryCache _memCache;

        #endregion

        public IMemoryCache Cache => _memCache;

        public EntityMemoryCache(MemoryCacheOptions options)
            : base(_defaultMemCacheOptions)
        {
            _memCache = new MemoryCache(options ?? _defaultMemCacheOptions);
        }
    }
}