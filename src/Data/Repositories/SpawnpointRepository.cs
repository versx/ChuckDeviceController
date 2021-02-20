namespace ChuckDeviceController.Data.Repositories
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.Extensions.Caching.Memory;
    using Z.EntityFramework.Plus;

    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Geofence.Models;

    public class SpawnpointRepository : EfCoreRepository<Spawnpoint, DeviceControllerContext>
    {
        public SpawnpointRepository(DeviceControllerContext context)
            : base(context)
        {
            QueryCacheManager.Cache = new MemoryCache(new MemoryCacheOptions());
        }

        public async Task<IReadOnlyList<Spawnpoint>> GetAllAsync(bool fromCache = true)
        {
            if (fromCache)
            {
                return _dbContext.Spawnpoints.FromCache().ToList();
            }
            return await base.GetAllAsync().ConfigureAwait(false);
        }

        public async Task<List<Spawnpoint>> GetAllAsync(BoundingBox bbox, ulong updated = 0)
        {
            var spawnpoints = await GetAllAsync(false).ConfigureAwait(false);
            return spawnpoints.Where(spawn =>
                spawn.Latitude >= bbox.MinimumLatitude &&
                spawn.Latitude <= bbox.MaximumLatitude &&
                spawn.Longitude >= bbox.MinimumLongitude &&
                spawn.Longitude <= bbox.MaximumLongitude &&
                spawn.Updated >= updated
            ).ToList();
        }
    }
}