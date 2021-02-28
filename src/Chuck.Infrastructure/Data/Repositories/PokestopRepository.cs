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
                    Name = cIns.Name != null ? cIns.Name : cDb.Name,
                    Url = cIns.Url != null ? cIns.Name : cDb.Name,
                    Enabled = cIns.Enabled != cDb.Enabled ? cIns.Enabled : cDb.Enabled,
                    LureExpireTimestamp = cIns.LureExpireTimestamp != null ? cIns.LureExpireTimestamp : cDb.LureExpireTimestamp,
                    LureId = cIns.LureId != null ? cIns.LureId : cDb.LureId,
                    PokestopDisplay = cIns.PokestopDisplay != null ? cIns.PokestopDisplay : cDb.PokestopDisplay,
                    IncidentExpireTimestamp = cIns.IncidentExpireTimestamp != null ? cIns.IncidentExpireTimestamp : cDb.IncidentExpireTimestamp,
                    GruntType = cIns.GruntType != null ? cIns.GruntType : cDb.GruntType,
                    QuestType = cIns.QuestType != null ? cIns.QuestType : cDb.QuestType,
                    QuestTimestamp = cIns.QuestTimestamp != null ? cIns.QuestTimestamp : cDb.QuestTimestamp,
                    QuestTarget = cIns.QuestTarget != null ? cIns.QuestTarget : cDb.QuestTarget,
                    QuestTemplate = cIns.QuestTemplate != null ? cIns.QuestTemplate : cDb.QuestTemplate,
                    QuestConditions = cIns.QuestConditions, // TODO: QuestCondtions
                    QuestRewards = cIns.QuestRewards, // TODO: QuestRewards
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
                    Name = cIns.Name != null ? cIns.Name : cDb.Name,
                    Url = cIns.Url != null ? cIns.Name : cDb.Name,
                    Enabled = cIns.Enabled != cDb.Enabled ? cIns.Enabled : cDb.Enabled,
                    LureExpireTimestamp = cIns.LureExpireTimestamp != null ? cIns.LureExpireTimestamp : cDb.LureExpireTimestamp,
                    LureId = cIns.LureId != 0 ? cIns.LureId : cDb.LureId,
                    PokestopDisplay = cIns.PokestopDisplay != null ? cIns.PokestopDisplay : cDb.PokestopDisplay,
                    IncidentExpireTimestamp = cIns.IncidentExpireTimestamp != null ? cIns.IncidentExpireTimestamp : cDb.IncidentExpireTimestamp,
                    GruntType = cIns.GruntType != null ? cIns.GruntType : cDb.GruntType,
                    QuestType = cIns.QuestType != null ? cIns.QuestType : cDb.QuestType,
                    QuestTimestamp = cIns.QuestTimestamp != null ? cIns.QuestTimestamp : cDb.QuestTimestamp,
                    QuestTarget = cIns.QuestTarget != null ? cIns.QuestTarget : cDb.QuestTarget,
                    QuestTemplate = cIns.QuestTemplate ?? cDb.QuestTemplate,
                    QuestConditions = cIns.QuestConditions, // TODO: QuestCondtions
                    QuestRewards = cIns.QuestRewards, // TODO: QuestRewards
                    Updated = now,
                    FirstSeenTimestamp = cDb.FirstSeenTimestamp == 0 ? now : cDb.FirstSeenTimestamp,
                })
                .RunAsync().ConfigureAwait(false);
        }

        public async Task AddOrUpdateAsync(List<Pokestop> entities, bool ignoreQuestFields = true)
        {
            try
            {
                await _dbContext.BulkMergeAsync(entities, x =>
                {
                    x.AutoMap = Z.BulkOperations.AutoMapType.ByIndexerName;
                    x.BatchSize = 100;
                    //x.BatchTimeout = 10 * 1000; // TODO: Seconds or ms?
                    x.InsertIfNotExists = true;
                    x.InsertKeepIdentity = true;
                    x.MergeKeepIdentity = true;
                    x.Resolution = Z.BulkOperations.ResolutionType.Smart;
                    x.UseTableLock = true; // TODO: ?
                    x.AllowDuplicateKeys = true; // TODO: ?
                    x.ColumnPrimaryKeyExpression = entity => entity.Id;
                    if (ignoreQuestFields)
                    {
                        x.IgnoreOnMergeUpdateExpression = p => new
                        {
                            p.QuestConditions,
                            p.QuestItemId,
                            p.QuestPokemonId,
                            p.QuestRewards,
                            p.QuestRewardType,
                            p.QuestTarget,
                            p.QuestTemplate,
                            p.QuestTimestamp,
                            p.QuestType,
                        };
                    }
                }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                ConsoleExt.WriteError($"[PokestopRepository] AddOrUpdateAsync: {ex}");
            }
        }

        public async Task ClearQuestsAsync()
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
        }

        public async Task<List<Pokestop>> GetAllAsync(BoundingBox bbox, ulong updated = 0)
        {
            var pokestops = await GetAllAsync(true).ConfigureAwait(false);
            return pokestops.Where(stop =>
                stop.Latitude >= bbox.MinimumLatitude &&
                stop.Latitude <= bbox.MaximumLatitude &&
                stop.Longitude >= bbox.MinimumLongitude &&
                stop.Longitude <= bbox.MaximumLongitude &&
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
            var pokestops = await GetByIdsAsync(ids).ConfigureAwait(false);
            return (ulong)pokestops.Where(x => !x.Deleted &&
                                               x.QuestType.HasValue &&
                                               x.QuestType != null).ToList().Count;
        }
    }
}