namespace ChuckDeviceController.Extensions.Http.Caching
{
    using Microsoft.Extensions.Caching.Memory;

    public class EntityMemoryCache : MemoryCache
    {
        public EntityMemoryCache(EntityMemoryCacheConfig config)
            : base(config)
        {
        }
    }
}