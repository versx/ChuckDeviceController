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
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AddOrUpdateAsync", ex);
            }
        }

        public async Task ClearQuestsAsync()
        {
            await _dbContext.Pokestops.UpdateFromQueryAsync(x => new Pokestop
            {
                QuestConditions = null,
                QuestRewards = null,
                QuestTarget = null,
                QuestTemplate = null,
                QuestTimestamp = null,
                QuestType = null,
            });
        }

        public async Task<List<Pokestop>> GetWithin(BoundingBox bbox, ulong updated = 0)
        {
            var pokestops = await GetAllAsync(true);
            return pokestops.Where(stop =>
                stop.Latitude >= bbox.MinimumLatitude &&
                stop.Latitude <= bbox.MaximumLatitude &&
                stop.Longitude >= bbox.MinimumLongitude &&
                stop.Longitude <= bbox.MaximumLongitude &&
                // TODO: stop.Updated >= updated &&
                !stop.Deleted
            ).ToList();
        }

        public async Task<IReadOnlyList<Pokestop>> GetAllAsync(bool fromCache = true)
        {
            if (fromCache)
            {
                return await Task.FromResult(_dbContext.Pokestops.FromCache().ToList());
            }
            return await base.GetAllAsync();
        }
    }
}