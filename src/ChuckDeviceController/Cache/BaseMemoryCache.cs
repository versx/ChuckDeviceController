namespace ChuckDeviceController.Cache
{
    using Microsoft.Extensions.Caching.Memory;

    public class BaseMemoryCache
    {
        public MemoryCache Cache { get; }

        public BaseMemoryCache(long sizeLimit = 1024)
        {
            Cache = new MemoryCache(new MemoryCacheOptions
            {
                SizeLimit = sizeLimit,
            });
        }
    }
}