namespace Chuck.Data.Repositories
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using System.Threading.Tasks;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Caching.Memory;
    using Z.EntityFramework.Plus;

    using Chuck.Data.Contexts;
    using Chuck.Data.Entities;
    using Chuck.Data.Specifications;
    using Chuck.Extensions;

    public class PokestopRepository : EfCoreRepository<Pokestop, DeviceControllerContext>
    {
        public PokestopRepository(DeviceControllerContext context)
            : base(context)
        {
            QueryCacheManager.Cache = new MemoryCache(new MemoryCacheOptions());
        }

        public async Task<int> InsertOrUpdate(Pokestop pokestop)
        {
            var now = DateTime.UtcNow.ToTotalSeconds();
            return await _dbContext.Pokestops
                .Upsert(pokestop)
                .On(p => p.Id)
                .WhenMatched((cDb, cIns) => new Pokestop
                {
                    Latitude = cIns.Latitude != cDb.Latitude ? cIns.Latitude : cDb.Latitude,
                    Longitude = cIns.Longitude != cDb.Longitude ? cIns.Longitude : cDb.Longitude,
                    Name = cIns.Name ?? cDb.Name,
                    Url = cIns.Url ?? cDb.Url,
                    Enabled = cIns.Enabled != cDb.Enabled ? cIns.Enabled : cDb.Enabled,
                    LureExpireTimestamp = cIns.LureExpireTimestamp ?? cDb.LureExpireTimestamp,
                    LureId = cIns.LureId != 0 ? cIns.LureId : cDb.LureId,
                    PokestopDisplay = cIns.PokestopDisplay ?? cDb.PokestopDisplay,
                    IncidentExpireTimestamp = cIns.IncidentExpireTimestamp ?? cDb.IncidentExpireTimestamp,
                    GruntType = cIns.GruntType ?? cDb.GruntType,
                    QuestType = cIns.QuestType ?? cDb.QuestType,
                    QuestTimestamp = cIns.QuestTimestamp ?? cDb.QuestTimestamp,
                    QuestTarget = cIns.QuestTarget ?? cDb.QuestTarget,
                    QuestTemplate = cIns.QuestTemplate ?? cDb.QuestTemplate,
                    QuestConditions = cIns.QuestConditions,
                    QuestRewards = cIns.QuestRewards,
                    Updated = now,
                    FirstSeenTimestamp = cDb.FirstSeenTimestamp == 0 ? now : cDb.FirstSeenTimestamp,
                })
                .RunAsync().ConfigureAwait(false);
        }

        public async Task<int> InsertOrUpdate(List<Pokestop> pokestops)
        {
            var now = DateTime.UtcNow.ToTotalSeconds();
            return await _dbContext.Pokestops
                .UpsertRange(pokestops)
                .On(p => p.Id)
                .WhenMatched((cDb, cIns) => new Pokestop
                {
                    Latitude = cIns.Latitude != cDb.Latitude ? cIns.Latitude : cDb.Latitude,
                    Longitude = cIns.Longitude != cDb.Longitude ? cIns.Longitude : cDb.Longitude,
                    Name = cIns.Name ?? cDb.Name,
                    Url = cIns.Url ?? cDb.Url,
                    Enabled = cIns.Enabled != cDb.Enabled ? cIns.Enabled : cDb.Enabled,
                    LureExpireTimestamp = cIns.LureExpireTimestamp ?? cDb.LureExpireTimestamp,
                    LureId = cIns.LureId != 0 ? cIns.LureId : cDb.LureId,
                    PokestopDisplay = cIns.PokestopDisplay ?? cDb.PokestopDisplay,
                    IncidentExpireTimestamp = cIns.IncidentExpireTimestamp ?? cDb.IncidentExpireTimestamp,
                    GruntType = cIns.GruntType ?? cDb.GruntType,
                    QuestType = cIns.QuestType ?? cDb.QuestType,
                    QuestTimestamp = cIns.QuestTimestamp ?? cDb.QuestTimestamp,
                    QuestTarget = cIns.QuestTarget ?? cDb.QuestTarget,
                    QuestTemplate = cIns.QuestTemplate ?? cDb.QuestTemplate,
                    QuestConditions = cIns.QuestConditions,
                    QuestRewards = cIns.QuestRewards,
                    Updated = now,
                    FirstSeenTimestamp = cDb.FirstSeenTimestamp == 0 ? now : cDb.FirstSeenTimestamp,
                })
                .RunAsync().ConfigureAwait(false);
        }

        public async Task ClearQuestsAsync(string instanceName = null)
        {
            if (string.IsNullOrEmpty(instanceName))
            {
                await _dbContext.Pokestops.UpdateFromQueryAsync(_ => new Pokestop
                {
                    QuestConditions = null,
                    QuestRewards = null,
                    QuestTarget = null,
                    QuestTemplate = null,
                    QuestTimestamp = null,
                    QuestType = null,
                }).ConfigureAwait(false);
                return;
            }

            // TODO: Clear quests per instance
            /*
            var instance = await new InstanceRepository(_dbContext).GetByIdAsync(instanceName).ConfigureAwait(false);
            if (instance == null)
            {
            }
            var geofence = await new GeofenceRepository(_dbContext).GetByIdAsync(instance.Geofence).ConfigureAwait(false);
            var area = geofence.Data.Area;
            var coordsArray = (List<List<Coordinate>>)
            (
                area is List<List<Coordinate>>
                    ? area
                    : JsonSerializer.Deserialize<List<List<Coordinate>>>(Convert.ToString(area))
            );
            var areaArrayEmptyInner = new List<MultiPolygon>();
            foreach (var coords in coordsArray)
            {
                var multiPolygon = new MultiPolygon();
                Coordinate first = null;
                for (var i = 0; i < coords.Count; i++)
                {
                    var coord = coords[i];
                    if (i == 0)
                    {
                        first = coord;
                    }
                    multiPolygon.Add(new Polygon(coord.Latitude, coord.Longitude));
                }
                if (first != null)
                {
                    multiPolygon.Add(new Polygon(first.Latitude, first.Longitude));
                }
                areaArrayEmptyInner.Add(multiPolygon);
            }
            var pokestops = await GetAllAsync(new Specification<Pokestop>(x =>
                x.QuestType != null &&
                GeofenceService.InMultiPolygon(areaArrayEmptyInner, x.Latitude, x.Longitude)
            )).ConfigureAwait(false);
            var result = await InsertOrUpdate(pokestops).ConfigureAwait(false);
            ConsoleExt.WriteDebug($"Result: {result}");
            */
        }

        public async Task<List<Pokestop>> GetAllAsync(double minLat, double minLon, double maxLat, double maxLon, ulong updated = 0)
        {
            var pokestops = await GetAllAsync(true).ConfigureAwait(false);
            return pokestops.Where(stop =>
                stop.Latitude >= minLat &&
                stop.Latitude <= maxLat &&
                stop.Longitude >= minLon &&
                stop.Longitude <= maxLon &&
                stop.Updated >= updated &&
                !stop.Deleted
            ).ToList();
        }

        public async Task<IReadOnlyList<Pokestop>> GetAllAsync(bool fromCache = true)
        {
            if (fromCache)
            {
                return await Task.FromResult(_dbContext.Pokestops.FromCache().ToList()).ConfigureAwait(false);
            }
            return await base.GetAllAsync().ConfigureAwait(false);
        }

        public async Task<ulong> GetQuestCount(List<string> ids)
        {
            const double MAX_COUNT = 10000.0;
            if (ids.Count > MAX_COUNT)
            {
                var result = 0UL;
                var count = Math.Ceiling(ids.Count / MAX_COUNT);
                for (var i = 0; i < count; i++)
                {
                    var start = (int)MAX_COUNT * i;
                    var end = (int)Math.Min(MAX_COUNT * (i + 1), ids.Count - 1);
                    var slice = ids.Slice(start, end);
                    var qResult = await GetQuestCount(slice).ConfigureAwait(false);
                    if (qResult > 0)
                    {
                        result += qResult;
                    }
                }
                return result;
            }
            if (ids.Count == 0)
            {
                return 0;
            }
            return (ulong)await _dbContext.Pokestops.AsNoTracking().DeferredCount(x =>
                !x.Deleted &&
                x.QuestType.HasValue &&
                x.QuestType != null &&
                ids.Contains(x.Id)
            ).FromCacheAsync().ConfigureAwait(false);
        }

        public async Task<int> GetStalePokestopsCount()
        {
            var staleTime = DateTime.UtcNow.Subtract(TimeSpan.FromHours(24)).ToTotalSeconds();
            return await _dbContext.Pokestops.AsNoTracking()
                                             .DeferredCount(x => x.Updated <= staleTime)
                                             .FromCacheAsync()
                                             .ConfigureAwait(false);
        }

        public async Task<int> DeleteStalePokestops()
        {
            var staleTime = DateTime.UtcNow.Subtract(TimeSpan.FromHours(24)).ToTotalSeconds();
            var stalePokestops = _dbContext.Pokestops.Where(x => x.Updated <= staleTime).ToList();
            await DeleteRangeAsync(stalePokestops);
            return stalePokestops.Count;
        }

        public async Task<int> GetConvertiblePokestopsCount()
        {
            var gymRepository = new GymRepository(_dbContext);
            var gyms = await gymRepository.GetAllAsync().ConfigureAwait(false);
            var gymIds = gyms.Select(x => x.Id);
            return await _dbContext.Pokestops.AsNoTracking()
                                             .DeferredCount(x => gymIds.Contains(x.Id))
                                             .FromCacheAsync()
                                             .ConfigureAwait(false);
        }
    }
}