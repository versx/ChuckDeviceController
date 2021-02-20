namespace ChuckDeviceController.Data.Repositories
{
    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Geofence.Models;
    using Microsoft.Extensions.Caching.Memory;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Z.EntityFramework.Plus;

    public class PokestopRepository : EfCoreRepository<Pokestop, DeviceControllerContext>
    {
        public PokestopRepository(DeviceControllerContext context)
            : base(context)
        {
            QueryCacheManager.Cache = new MemoryCache(new MemoryCacheOptions());
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
                Console.WriteLine($"[PokestopRepository] AddOrUpdateAsync: {ex}");
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
            IReadOnlyList<Pokestop> pokestops = await GetAllAsync(true).ConfigureAwait(false);
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
    }
}