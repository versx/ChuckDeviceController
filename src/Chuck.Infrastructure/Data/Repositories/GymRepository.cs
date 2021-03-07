namespace Chuck.Infrastructure.Data.Repositories
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.EntityFrameworkCore;
    using Z.EntityFramework.Plus;

    using Chuck.Infrastructure.Data.Contexts;
    using Chuck.Infrastructure.Data.Entities;
    using Chuck.Infrastructure.Extensions;
    using Chuck.Infrastructure.Geofence.Models;

    public class GymRepository : EfCoreRepository<Gym, DeviceControllerContext>
    {
        public GymRepository(DeviceControllerContext context)
            : base(context)
        {
        }

        public async Task<int> InsertOrUpdate(Gym gym)
        {
            var now = DateTime.UtcNow.ToTotalSeconds();
            return await _dbContext.Gyms
                .Upsert(gym)
                .On(p => p.Id)
                .WhenMatched((cDb, cIns) => new Gym
                {
                    Latitude = cIns.Latitude != cDb.Latitude ? cIns.Latitude : cDb.Latitude,
                    Longitude = cIns.Longitude != cDb.Longitude ? cIns.Longitude : cDb.Longitude,
                    AvailableSlots = cIns.AvailableSlots,
                    Deleted = cIns.Deleted,
                    Enabled = cIns.Enabled,
                    Team = cIns.Team,
                    GuardingPokemonId = cIns.GuardingPokemonId,
                    InBattle = cIns.InBattle,
                    ExRaidEligible = cIns.ExRaidEligible,
                    SponsorId = cIns.SponsorId,
                    LastModifiedTimestamp = cIns.LastModifiedTimestamp,
                    TotalCP = cIns.TotalCP,
                    Name = cIns.Name ?? cDb.Name,
                    Url = cIns.Url ?? cDb.Url,
                    CellId = cIns.CellId,
                    RaidBattleTimestamp = cIns.RaidBattleTimestamp != null ? cIns.RaidBattleTimestamp : cDb.RaidBattleTimestamp,
                    RaidEndTimestamp = cIns.RaidEndTimestamp != null ? cIns.RaidEndTimestamp : cDb.RaidEndTimestamp,
                    RaidSpawnTimestamp = cIns.RaidBattleTimestamp != null ? cIns.RaidBattleTimestamp : cDb.RaidBattleTimestamp,
                    RaidIsExclusive = cIns.RaidIsExclusive,
                    RaidLevel = cIns.RaidLevel,
                    RaidPokemonCostume = cIns.RaidPokemonCostume,
                    RaidPokemonCP = cIns.RaidPokemonCP != null ? cIns.RaidPokemonCP : cDb.RaidPokemonCP,
                    RaidPokemonEvolution = cIns.RaidPokemonEvolution,
                    RaidPokemonForm = cIns.RaidPokemonForm != null ? cIns.RaidPokemonForm : cDb.RaidPokemonForm,
                    RaidPokemonGender = cIns.RaidPokemonGender != null ? cIns.RaidPokemonGender : cDb.RaidPokemonGender,
                    RaidPokemonId = cIns.RaidPokemonId != null ? cIns.RaidPokemonId : cDb.RaidPokemonId,
                    RaidPokemonMove1 = cIns.RaidPokemonMove1 != null ? cIns.RaidPokemonMove1 : cDb.RaidPokemonMove1,
                    RaidPokemonMove2 = cIns.RaidPokemonMove2 != null ? cIns.RaidPokemonMove2 : cDb.RaidPokemonMove2,
                    Updated = now,
                    FirstSeenTimestamp = cDb.FirstSeenTimestamp == 0 ? now : cDb.FirstSeenTimestamp,
                })
                .RunAsync().ConfigureAwait(false);
        }

        public async Task<int> InsertOrUpdate(List<Gym> gyms)
        {
            var now = DateTime.UtcNow.ToTotalSeconds();
            return await _dbContext.Gyms
                .UpsertRange(gyms)
                .On(p => p.Id)
                .WhenMatched((cDb, cIns) => new Gym
                {
                    Latitude = cIns.Latitude != cDb.Latitude ? cIns.Latitude : cDb.Latitude,
                    Longitude = cIns.Longitude != cDb.Longitude ? cIns.Longitude : cDb.Longitude,
                    AvailableSlots = cIns.AvailableSlots,
                    Deleted = cIns.Deleted,
                    Enabled = cIns.Enabled,
                    Team = cIns.Team,
                    GuardingPokemonId = cIns.GuardingPokemonId,
                    InBattle = cIns.InBattle,
                    ExRaidEligible = cIns.ExRaidEligible,
                    SponsorId = cIns.SponsorId,
                    LastModifiedTimestamp = cIns.LastModifiedTimestamp,
                    TotalCP = cIns.TotalCP,
                    Name = cIns.Name ?? cDb.Name,
                    Url = cIns.Url ?? cDb.Url,
                    CellId = cIns.CellId,
                    RaidBattleTimestamp = cIns.RaidBattleTimestamp != null ? cIns.RaidBattleTimestamp : cDb.RaidBattleTimestamp,
                    RaidEndTimestamp = cIns.RaidEndTimestamp != null ? cIns.RaidEndTimestamp : cDb.RaidEndTimestamp,
                    RaidSpawnTimestamp = cIns.RaidBattleTimestamp != null ? cIns.RaidBattleTimestamp : cDb.RaidBattleTimestamp,
                    RaidIsExclusive = cIns.RaidIsExclusive,
                    RaidLevel = cIns.RaidLevel,
                    RaidPokemonCostume = cIns.RaidPokemonCostume,
                    RaidPokemonCP = cIns.RaidPokemonCP != null ? cIns.RaidPokemonCP : cDb.RaidPokemonCP,
                    RaidPokemonEvolution = cIns.RaidPokemonEvolution,
                    RaidPokemonForm = cIns.RaidPokemonForm != null ? cIns.RaidPokemonForm : cDb.RaidPokemonForm,
                    RaidPokemonGender = cIns.RaidPokemonGender != null ? cIns.RaidPokemonGender : cDb.RaidPokemonGender,
                    RaidPokemonId = cIns.RaidPokemonId != null ? cIns.RaidPokemonId : cDb.RaidPokemonId,
                    RaidPokemonMove1 = cIns.RaidPokemonMove1 != null ? cIns.RaidPokemonMove1 : cDb.RaidPokemonMove1,
                    RaidPokemonMove2 = cIns.RaidPokemonMove2 != null ? cIns.RaidPokemonMove2 : cDb.RaidPokemonMove2,
                    Updated = now,
                    FirstSeenTimestamp = cDb.FirstSeenTimestamp == 0 ? now : cDb.FirstSeenTimestamp,
                })
                .RunAsync().ConfigureAwait(false);
        }

        public async Task<List<Gym>> GetAllAsync(BoundingBox bbox, ulong updated = 0)
        {
            var gyms = await GetAllAsync(true).ConfigureAwait(false);
            return gyms.Where(gym =>
                gym.Latitude >= bbox.MinimumLatitude &&
                gym.Latitude <= bbox.MaximumLatitude &&
                gym.Longitude >= bbox.MinimumLongitude &&
                gym.Longitude <= bbox.MaximumLongitude &&
                gym.Updated >= updated &&
                !gym.Deleted
            ).ToList();
        }

        public async Task<IReadOnlyList<Gym>> GetAllAsync(bool fromCache = true)
        {
            if (fromCache)
            {
                return await Task.FromResult(_dbContext.Gyms.FromCache().ToList()).ConfigureAwait(false);
            }
            return await base.GetAllAsync().ConfigureAwait(false);
        }
    }
}