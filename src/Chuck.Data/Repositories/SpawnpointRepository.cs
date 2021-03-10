namespace Chuck.Data.Repositories
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Caching.Memory;
    using Z.EntityFramework.Plus;

    using Chuck.Data.Contexts;
    using Chuck.Data.Entities;
    using Chuck.Extensions;

    using Pomelo.EntityFrameworkCore.MySql;

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

        public async Task<List<Spawnpoint>> GetAllAsync(double minLat, double minLon, double maxLat, double maxLon, bool onlyUnknown, ulong updated = 0)
        {
            var spawnpoints = await GetAllAsync(false).ConfigureAwait(false);
            return spawnpoints.Where(spawn =>
                spawn.Latitude >= minLat &&
                spawn.Latitude <= maxLat &&
                spawn.Longitude >= minLon &&
                spawn.Longitude <= maxLon &&
                (spawn.DespawnSecond == null && onlyUnknown) &&
                spawn.Updated >= updated
            ).ToList();
        }
    }
}