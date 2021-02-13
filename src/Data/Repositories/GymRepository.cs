namespace ChuckDeviceController.Data.Repositories
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Z.EntityFramework.Plus;

    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Data.Entities;

    public class GymRepository : EfCoreRepository<Gym, DeviceControllerContext>
    {
        public GymRepository(DeviceControllerContext context)
            : base(context)
        {
        }

        public async Task AddOrUpdateAsync(List<Gym> entities, bool ignoreRaidFields) // = true
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
                    if (ignoreRaidFields)
                    {
                        x.IgnoreOnMergeUpdateExpression = p => new
                        {
                            p.RaidBattleTimestamp,
                            p.RaidEndTimestamp,
                            p.RaidIsExclusive,
                            p.RaidLevel,
                            p.RaidPokemonCostume,
                            p.RaidPokemonCP,
                            p.RaidPokemonEvolution,
                            p.RaidPokemonForm,
                            p.RaidPokemonGender,
                            p.RaidPokemonId,
                            p.RaidPokemonMove1,
                            p.RaidPokemonMove2,
                            p.RaidSpawnTimestamp,
                        };
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AddOrUpdateAsync", ex);
            }
        }

        public async Task<IReadOnlyList<Gym>> GetAllAsync(bool fromCache = true)
        {
            if (fromCache)
            {
                return await Task.FromResult(_dbContext.Gyms.FromCache().ToList());
            }
            return await base.GetAllAsync();
        }
    }
}