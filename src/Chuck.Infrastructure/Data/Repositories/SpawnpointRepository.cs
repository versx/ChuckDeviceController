namespace Chuck.Infrastructure.Data.Repositories
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Caching.Memory;
    using Z.EntityFramework.Plus;

    using Chuck.Infrastructure.Data.Contexts;
    using Chuck.Infrastructure.Data.Entities;
    using Chuck.Infrastructure.Extensions;
    using Chuck.Infrastructure.Geofence.Models;

    public class SpawnpointRepository : EfCoreRepository<Spawnpoint, DeviceControllerContext>
    {
        public SpawnpointRepository(DeviceControllerContext context)
            : base(context)
        {
            QueryCacheManager.Cache = new MemoryCache(new MemoryCacheOptions());
        }

        public async Task<int> InsertOrUpdate(Spawnpoint spawnpoint)
        {
            var now = DateTime.UtcNow.ToTotalSeconds();
            return await _dbContext.Spawnpoints
                .Upsert(spawnpoint)
                .On(p => p.Id)
                .WhenMatched((cDb, cIns) => new Spawnpoint
                {
                    DespawnSecond = cIns.DespawnSecond != null
                        ? cIns.DespawnSecond
                        : cDb.DespawnSecond,
                    FirstSeenTimestamp = cDb.FirstSeenTimestamp == 0 && cIns.FirstSeenTimestamp == 0 ? now : cDb.FirstSeenTimestamp,
                    Updated = now,
                })
                .RunAsync().ConfigureAwait(false);
        }

        public async Task<int> InsertOrUpdate(List<Spawnpoint> spawnpoints)
        {
            var now = DateTime.UtcNow.ToTotalSeconds();
            return await _dbContext.Spawnpoints
                .UpsertRange(spawnpoints)
                .On(p => p.Id)
                .WhenMatched((cDb, cIns) => new Spawnpoint
                {
                    DespawnSecond = cIns.DespawnSecond != null
                        ? cIns.DespawnSecond
                        : cDb.DespawnSecond,
                    FirstSeenTimestamp = cDb.FirstSeenTimestamp == 0 && cIns.FirstSeenTimestamp == 0 ? now : cDb.FirstSeenTimestamp,
                    Updated = now,
                })
                .RunAsync().ConfigureAwait(false);
        }

        public async Task<IReadOnlyList<Spawnpoint>> GetAllAsync(bool fromCache = true)
        {
            if (fromCache)
            {
                return _dbContext.Spawnpoints.FromCache().ToList();
            }
            return await base.GetAllAsync().ConfigureAwait(false);
        }

        public async Task<List<Spawnpoint>> GetAllAsync(BoundingBox bbox, bool onlyUnknown, ulong updated = 0)
        {
            var spawnpoints = await GetAllAsync(false).ConfigureAwait(false);
            return spawnpoints.Where(spawn =>
                spawn.Latitude >= bbox.MinimumLatitude &&
                spawn.Latitude <= bbox.MaximumLatitude &&
                spawn.Longitude >= bbox.MinimumLongitude &&
                spawn.Longitude <= bbox.MaximumLongitude &&
                (spawn.DespawnSecond == null && onlyUnknown) &&
                spawn.Updated >= updated
            ).ToList();
        }
    }
}