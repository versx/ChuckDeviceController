namespace Chuck.Data.Repositories
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.Extensions.Caching.Memory;
    using Z.EntityFramework.Plus;

    using Chuck.Data.Contexts;
    using Chuck.Data.Entities;

    public class GeofenceRepository : EfCoreRepository<Geofence, DeviceControllerContext>
    {
        public GeofenceRepository(DeviceControllerContext context)
            : base(context)
        {
            QueryCacheManager.Cache = new MemoryCache(new MemoryCacheOptions());
        }

        public async Task<IReadOnlyList<Geofence>> GetAllAsync(bool fromCache = true)
        {
            if (fromCache)
            {
                return (IReadOnlyList<Geofence>)await Task.FromResult(_dbContext.Geofences.FromCache().ToList()).ConfigureAwait(false);
            }
            return await base.GetAllAsync().ConfigureAwait(false);
        }
    }
}