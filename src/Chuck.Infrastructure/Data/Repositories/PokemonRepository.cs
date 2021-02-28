namespace Chuck.Infrastructure.Data.Repositories
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.EntityFrameworkCore;

    using Chuck.Infrastructure.Data.Contexts;
    using Chuck.Infrastructure.Data.Entities;
    using Chuck.Infrastructure.Extensions;

    public class PokemonRepository : EfCoreRepository<Pokemon, DeviceControllerContext>
    {
        public PokemonRepository(DeviceControllerContext context)
            : base(context)
        {
        }

        public async Task<int> InsertOrUpdate(Pokemon pokemon)
        {
            var now = DateTime.UtcNow.ToTotalSeconds();
            return await _dbContext.Pokemon
                .Upsert(pokemon)
                .On(p => p.Id)
                .WhenMatched((cDb, cIns) => new Pokemon
                {
                    CellId = cIns.CellId,
                    PokemonId = cIns.PokemonId,
                    Gender = cIns.Gender,
                    Form = cDb.Form == null
                        ? cIns.Form
                        : cDb.Form,
                    Costume = cIns.Costume,
                    CaptureRate1 = cDb.CaptureRate1 == null
                        ? cIns.CaptureRate1
                        : cDb.CaptureRate1,
                    CaptureRate2 = cDb.CaptureRate2 == null
                        ? cIns.CaptureRate2
                        : cDb.CaptureRate2,
                    CaptureRate3 = cDb.CaptureRate3 == null
                        ? cIns.CaptureRate3
                        : cDb.CaptureRate3,
                    CP = cDb.CP == null
                        ? cIns.CP
                        : cDb.CP,
                    Level = cIns.Level,
                    AttackIV = cDb.AttackIV == null
                        ? cIns.AttackIV
                        : cDb.AttackIV,
                    DefenseIV = cDb.DefenseIV == null
                        ? cIns.DefenseIV
                        : cDb.DefenseIV,
                    StaminaIV = cIns.StaminaIV == null
                        ? cIns.StaminaIV
                        : cDb.StaminaIV,
                    DisplayPokemonId = cDb.DisplayPokemonId == null
                        ? cIns.DisplayPokemonId
                        : cDb.DisplayPokemonId,
                    Weather = cIns.Weather,
                    Weight = cDb.Weight == null
                        ? cIns.Weight
                        : cDb.Weight,
                    Size = cDb.Size == null
                        ? cIns.Size
                        : cDb.Size,
                    Username = cIns.Username,
                    IsShiny = cIns.IsShiny,
                    ExpireTimestamp = cIns.ExpireTimestamp,
                    IsExpireTimestampVerified = cIns.IsExpireTimestampVerified,
                    Move1 = cDb.Move1 == null
                        ? cIns.Move1
                        : cDb.Move1,
                    Move2 = cDb.Move2 == null
                        ? cIns.Move2
                        : cDb.Move2,
                    PokestopId = cDb.PokestopId == null
                        ? cIns.PokestopId
                        : cDb.PokestopId,
                    SpawnId = cDb.SpawnId == null
                        ? cIns.SpawnId
                        : cDb.SpawnId,
                    Changed = cIns.Changed,
                    PvpRankingsGreatLeague = cDb.PvpRankingsGreatLeague == null
                        ? cIns.PvpRankingsGreatLeague
                        : cDb.PvpRankingsGreatLeague,
                    PvpRankingsUltraLeague = cDb.PvpRankingsUltraLeague == null
                        ? cIns.PvpRankingsUltraLeague
                        : cDb.PvpRankingsUltraLeague,
                    Updated = now,
                    FirstSeenTimestamp = cDb.FirstSeenTimestamp == 0 ? now : cDb.FirstSeenTimestamp,
                })
                .RunAsync().ConfigureAwait(false);
        }

        public async Task<int> InsertOrUpdate(List<Pokemon> pokemon)
        {
            var now = DateTime.UtcNow.ToTotalSeconds();
            return await _dbContext.Pokemon
                .UpsertRange(pokemon)
                .On(p => p.Id)
                .WhenMatched((cDb, cIns) => new Pokemon
                {
                    CellId = cIns.CellId,
                    PokemonId = cIns.PokemonId,
                    Gender = cIns.Gender,
                    Form = cDb.Form == null
                        ? cIns.Form
                        : cDb.Form,
                    Costume = cIns.Costume,
                    CaptureRate1 = cDb.CaptureRate1 == null
                        ? cIns.CaptureRate1
                        : cDb.CaptureRate1,
                    CaptureRate2 = cDb.CaptureRate2 == null
                        ? cIns.CaptureRate2
                        : cDb.CaptureRate2,
                    CaptureRate3 = cDb.CaptureRate3 == null
                        ? cIns.CaptureRate3
                        : cDb.CaptureRate3,
                    CP = cDb.CP == null
                        ? cIns.CP
                        : cDb.CP,
                    Level = cIns.Level,
                    AttackIV = cDb.AttackIV == null
                        ? cIns.AttackIV
                        : cDb.AttackIV,
                    DefenseIV = cDb.DefenseIV == null
                        ? cIns.DefenseIV
                        : cDb.DefenseIV,
                    StaminaIV = cIns.StaminaIV == null
                        ? cIns.StaminaIV
                        : cDb.StaminaIV,
                    DisplayPokemonId = cDb.DisplayPokemonId == null
                        ? cIns.DisplayPokemonId
                        : cDb.DisplayPokemonId,
                    Weather = cIns.Weather,
                    Weight = cDb.Weight == null
                        ? cIns.Weight
                        : cDb.Weight,
                    Size = cDb.Size == null
                        ? cIns.Size
                        : cDb.Size,
                    Username = cIns.Username,
                    IsShiny = cIns.IsShiny,
                    ExpireTimestamp = cIns.ExpireTimestamp,
                    IsExpireTimestampVerified = cIns.IsExpireTimestampVerified,
                    Move1 = cDb.Move1 == null
                        ? cIns.Move1
                        : cDb.Move1,
                    Move2 = cDb.Move2 == null
                        ? cIns.Move2
                        : cDb.Move2,
                    PokestopId = cDb.PokestopId == null
                        ? cIns.PokestopId
                        : cDb.PokestopId,
                    SpawnId = cDb.SpawnId == null
                        ? cIns.SpawnId
                        : cDb.SpawnId,
                    Changed = cIns.Changed,
                    PvpRankingsGreatLeague = cDb.PvpRankingsGreatLeague == null
                        ? cIns.PvpRankingsGreatLeague
                        : cDb.PvpRankingsGreatLeague,
                    PvpRankingsUltraLeague = cDb.PvpRankingsUltraLeague == null
                        ? cIns.PvpRankingsUltraLeague
                        : cDb.PvpRankingsUltraLeague,
                    Updated = now,
                    FirstSeenTimestamp = cDb.FirstSeenTimestamp == 0 ? now : cDb.FirstSeenTimestamp,
                })
                .RunAsync().ConfigureAwait(false);
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
                ConsoleExt.WriteError($"[PokemonRepository] AddOrUpdateAsync: {ex}");
            }
        }
    }
}