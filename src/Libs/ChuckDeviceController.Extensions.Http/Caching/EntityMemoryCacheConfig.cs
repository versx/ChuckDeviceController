namespace ChuckDeviceController.Extensions.Http.Caching
{
    using Microsoft.Extensions.Caching.Memory;

    public class EntityMemoryCacheConfig : MemoryCacheOptions
    {
        public ushort EntityExpiryLimitM { get; set; }

        public IReadOnlyList<string> EntityTypeNames { get; set; }

        public EntityMemoryCacheConfig()
        {
            // Default: 0.05
            CompactionPercentage = 0.25;
            EntityExpiryLimitM = 15;
            EntityTypeNames = new List<string>();
            // Default: 1 minute
            ExpirationScanFrequency = TimeSpan.FromMinutes(1);
            // Default: 1 MB (1 * 1024 * 1024)
            // Default: 200 MB (200 * 1024 * 1024)
            // Reference: https://github.com/dotnet/runtime/blob/main/src/libraries/Microsoft.Extensions.Caching.Memory/src/MemoryCacheOptions.cs
            // Reference: https://github.com/dotnet/runtime/blob/main/src/libraries/Microsoft.Extensions.Caching.Memory/src/MemoryDistributedCacheOptions.cs
            SizeLimit = 10240;
        }
    }
}