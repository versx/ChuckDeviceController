﻿namespace Chuck.Data.Repositories
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.EntityFrameworkCore;

    using Chuck.Data.Contexts;
    using Chuck.Data.Entities;
    using Chuck.Extensions;

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
                    Form = cIns.Form ?? cDb.Form,
                    Costume = cIns.Costume,
                    CP = cIns.CP ?? cDb.CP,
                    Level = cIns.Level,
                    AttackIV = cIns.AttackIV ?? cDb.AttackIV,
                    DefenseIV = cIns.DefenseIV ?? cDb.DefenseIV,
                    StaminaIV = cIns.StaminaIV ?? cDb.StaminaIV,
                    DisplayPokemonId = cIns.DisplayPokemonId ?? cDb.DisplayPokemonId,
                    Weather = cIns.Weather,
                    Weight = cIns.Weight ?? cDb.Weight,
                    Size = cIns.Size ?? cDb.Size,
                    Username = cIns.Username,
                    IsShiny = cIns.IsShiny,
                    ExpireTimestamp = cIns.ExpireTimestamp,
                    IsExpireTimestampVerified = cIns.IsExpireTimestampVerified,
                    Move1 = cIns.Move1 ?? cDb.Move1,
                    Move2 = cIns.Move2 ?? cDb.Move2,
                    PokestopId = cIns.PokestopId ?? cDb.PokestopId,
                    SpawnId = cIns.SpawnId ?? cDb.SpawnId,
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
                    Form = cIns.Form ?? cDb.Form,
                    Costume = cIns.Costume,
                    CP = cIns.CP ?? cDb.CP,
                    Level = cIns.Level,
                    AttackIV = cIns.AttackIV ?? cDb.AttackIV,
                    DefenseIV = cIns.DefenseIV ?? cDb.DefenseIV,
                    StaminaIV = cIns.StaminaIV ?? cDb.StaminaIV,
                    DisplayPokemonId = cIns.DisplayPokemonId ?? cDb.DisplayPokemonId,
                    Weather = cIns.Weather,
                    Weight = cIns.Weight ?? cDb.Weight,
                    Size = cIns.Size ?? cDb.Size,
                    Username = cIns.Username,
                    IsShiny = cIns.IsShiny,
                    ExpireTimestamp = cIns.ExpireTimestamp,
                    IsExpireTimestampVerified = cIns.IsExpireTimestampVerified,
                    Move1 = cIns.Move1 ?? cDb.Move1,
                    Move2 = cIns.Move2 ?? cDb.Move2,
                    PokestopId = cIns.PokestopId ?? cDb.PokestopId,
                    SpawnId = cIns.SpawnId ?? cDb.SpawnId,
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