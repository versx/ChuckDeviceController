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
                    Form = cIns.Form != null
                        ? cIns.Form
                        : cDb.Form,
                    Costume = cIns.Costume,
                    CaptureRate1 = cIns.CaptureRate1 != null
                        ? cIns.CaptureRate1
                        : cDb.CaptureRate1,
                    CaptureRate2 = cIns.CaptureRate2 != null
                        ? cIns.CaptureRate2
                        : cDb.CaptureRate2,
                    CaptureRate3 = cIns.CaptureRate3 != null
                        ? cIns.CaptureRate3
                        : cDb.CaptureRate3,
                    CP = cDb.CP == null
                        ? cIns.CP
                        : cDb.CP,
                    Level = cIns.Level,
                    AttackIV = cIns.AttackIV != null
                        ? cIns.AttackIV
                        : cDb.AttackIV,
                    DefenseIV = cIns.DefenseIV != null
                        ? cIns.DefenseIV
                        : cDb.DefenseIV,
                    StaminaIV = cIns.StaminaIV != null
                        ? cIns.StaminaIV
                        : cDb.StaminaIV,
                    DisplayPokemonId = cIns.DisplayPokemonId != null
                        ? cIns.DisplayPokemonId
                        : cDb.DisplayPokemonId,
                    Weather = cIns.Weather,
                    Weight = cIns.Weight != null
                        ? cIns.Weight
                        : cDb.Weight,
                    Size = cIns.Size != null
                        ? cIns.Size
                        : cDb.Size,
                    Username = cIns.Username,
                    IsShiny = cIns.IsShiny,
                    ExpireTimestamp = cIns.ExpireTimestamp,
                    IsExpireTimestampVerified = cIns.IsExpireTimestampVerified,
                    Move1 = cIns.Move1 != null
                        ? cIns.Move1
                        : cDb.Move1,
                    Move2 = cIns.Move2 != null
                        ? cIns.Move2
                        : cDb.Move2,
                    PokestopId = cIns.PokestopId ?? cDb.PokestopId,
                    SpawnId = cIns.SpawnId != null
                        ? cIns.SpawnId
                        : cDb.SpawnId,
                    Changed = cIns.Changed,
                    PvpRankingsGreatLeague = cIns.PvpRankingsGreatLeague ?? cDb.PvpRankingsGreatLeague,
                    PvpRankingsUltraLeague = cIns.PvpRankingsUltraLeague ?? cDb.PvpRankingsUltraLeague,
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
                    Form = cIns.Form != null
                        ? cIns.Form
                        : cDb.Form,
                    Costume = cIns.Costume,
                    CaptureRate1 = cIns.CaptureRate1 != null
                        ? cIns.CaptureRate1
                        : cDb.CaptureRate1,
                    CaptureRate2 = cIns.CaptureRate2 != null
                        ? cIns.CaptureRate2
                        : cDb.CaptureRate2,
                    CaptureRate3 = cIns.CaptureRate3 != null
                        ? cIns.CaptureRate3
                        : cDb.CaptureRate3,
                    CP = cDb.CP == null
                        ? cIns.CP
                        : cDb.CP,
                    Level = cIns.Level,
                    AttackIV = cIns.AttackIV != null
                        ? cIns.AttackIV
                        : cDb.AttackIV,
                    DefenseIV = cIns.DefenseIV != null
                        ? cIns.DefenseIV
                        : cDb.DefenseIV,
                    StaminaIV = cIns.StaminaIV != null
                        ? cIns.StaminaIV
                        : cDb.StaminaIV,
                    DisplayPokemonId = cIns.DisplayPokemonId != null
                        ? cIns.DisplayPokemonId
                        : cDb.DisplayPokemonId,
                    Weather = cIns.Weather,
                    Weight = cIns.Weight != null
                        ? cIns.Weight
                        : cDb.Weight,
                    Size = cIns.Size != null
                        ? cIns.Size
                        : cDb.Size,
                    Username = cIns.Username,
                    IsShiny = cIns.IsShiny,
                    ExpireTimestamp = cIns.ExpireTimestamp,
                    IsExpireTimestampVerified = cIns.IsExpireTimestampVerified,
                    Move1 = cIns.Move1 != null
                        ? cIns.Move1
                        : cDb.Move1,
                    Move2 = cIns.Move2 != null
                        ? cIns.Move2
                        : cDb.Move2,
                    PokestopId = cIns.PokestopId ?? cDb.PokestopId,
                    SpawnId = cIns.SpawnId != null
                        ? cIns.SpawnId
                        : cDb.SpawnId,
                    Changed = cIns.Changed,
                    PvpRankingsGreatLeague = cIns.PvpRankingsGreatLeague ?? cDb.PvpRankingsGreatLeague,
                    PvpRankingsUltraLeague = cIns.PvpRankingsUltraLeague ?? cDb.PvpRankingsUltraLeague,
                    Updated = now,
                    FirstSeenTimestamp = cDb.FirstSeenTimestamp == 0 ? now : cDb.FirstSeenTimestamp,
                })
                .RunAsync().ConfigureAwait(false);
        }

        public async Task Truncate()
        {
            await _dbContext.Pokemon.BulkDeleteAsync(_dbContext.Pokemon);
        }
    }
}