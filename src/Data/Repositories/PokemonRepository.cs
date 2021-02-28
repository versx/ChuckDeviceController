namespace ChuckDeviceController.Data.Repositories
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Extensions;

    public class PokemonRepository : EfCoreRepository<Pokemon, DeviceControllerContext>
    {
        public PokemonRepository(DeviceControllerContext context)
            : base(context)
        {
        }

        public async Task AddOrUpdateAsync(List<Pokemon> entities, bool ignoreIvFields = true, bool ignoreTimeFields = true)
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
                    if (ignoreIvFields)
                    {
                        x.IgnoreOnMergeUpdateExpression = p => new
                        {
                            p.AttackIV,
                            p.DefenseIV,
                            p.StaminaIV,
                            p.Form,
                            p.Costume,
                            p.Gender,
                            p.CP,
                            p.CaptureRate1,
                            p.CaptureRate2,
                            p.CaptureRate3,
                            p.Move1,
                            p.Move2,
                        };
                    }
                    if (ignoreTimeFields)
                    {
                        x.IgnoreOnMergeUpdateExpression = p => new
                        {
                            p.FirstSeenTimestamp,
                            //p.ExpireTimestamp,
                            //p.Changed,
                            //p.Updated,
                        };
                    }
                }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("foreign key constraint fails"))
                {
                    // noting..
                    //ConsoleExt.WriteError($"[PokemonRepository] AddOrUpdateAsync: {ex.Message}");
                }
                else
                {
                    ConsoleExt.WriteError($"[PokemonRepository] AddOrUpdateAsync: {ex.Message}");
                }
            }
        }
    }
}