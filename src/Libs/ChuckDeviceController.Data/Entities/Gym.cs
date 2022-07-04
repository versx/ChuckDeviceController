namespace ChuckDeviceController.Data.Entities
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    using POGOProtos.Rpc;

    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Extensions;

    [Table("gym")]
    public class Gym : BaseEntity
    {
        public const ushort ExRaidBossId = 150;
        public const ushort ExRaidBossForm = 0;

        #region Properties

        [
            Column("id"),
            Key,
            DatabaseGenerated(DatabaseGeneratedOption.None),
        ]
        public string Id { get; set; }

        [Column("lat")]
        public double Latitude { get; set; }

        [Column("lon")]
        public double Longitude { get; set; }

        [Column("name")]
        public string? Name { get; set; }

        [Column("url")]
        public string? Url { get; set; }

        [Column("last_modified_timestamp")]
        public ulong LastModifiedTimestamp { get; set; }

        [Column("raid_end_timestamp")]
        public ulong? RaidEndTimestamp { get; set; }

        [Column("raid_spawn_timestamp")]
        public ulong? RaidSpawnTimestamp { get; set; }

        [Column("raid_battle_timestamp")]
        public ulong? RaidBattleTimestamp { get; set; }

        [Column("updated")]
        public ulong Updated { get; set; }

        [Column("raid_pokemon_id")]
        public uint? RaidPokemonId { get; set; }

        [Column("guarding_pokemon_id")]
        public uint GuardingPokemonId { get; set; }

        [Column("available_slots")]
        public ushort AvailableSlots { get; set; }

        [Column("team_id")]
        public Team Team { get; set; }

        [Column("raid_level")]
        public ushort? RaidLevel { get; set; }

        [Column("enabled")]
        public bool Enabled { get; set; }

        [Column("ex_raid_eligible")]
        public bool IsExRaidEligible { get; set; }

        [Column("in_battle")]
        public bool InBattle { get; set; }

        [Column("raid_pokemon_move_1")]
        public uint? RaidPokemonMove1 { get; set; }

        [Column("raid_pokemon_move_2")]
        public uint? RaidPokemonMove2 { get; set; }

        [Column("raid_pokemon_form")]
        public uint? RaidPokemonForm { get; set; }

        [Column("raid_pokemon_costume")]
        public uint? RaidPokemonCostume { get; set; }

        [Column("raid_pokemon_cp")]
        public uint? RaidPokemonCP { get; set; }

        [Column("raid_pokemon_evolution")]
        public uint? RaidPokemonEvolution { get; set; }

        [Column("raid_pokemon_gender")]
        public ushort? RaidPokemonGender { get; set; }

        [Column("raid_is_exclusive")]
        public bool? RaidIsExclusive { get; set; }

        [Column("cell_id")]
        public ulong CellId { get; set; }

        [Column("deleted")]
        public bool IsDeleted { get; set; }

        [Column("total_cp")]
        public int TotalCP { get; set; }

        [Column("first_seen_timestamp")]
        public ulong FirstSeenTimestamp { get; set; }

        [Column("sponsor_id")]
        public uint? SponsorId { get; set; }

        [Column("ar_scan_eligible")]
        public bool? IsArScanEligible { get; set; }

        [Column("power_up_points")]
        public uint? PowerUpPoints { get; set; }

        [Column("power_up_level")]
        public ushort? PowerUpLevel { get; set; }

        [Column("power_up_end_timestamp")]
        public ulong? PowerUpEndTimestamp { get; set; }

        [NotMapped]
        public bool HasChanges { get; set; }

        #endregion

        public Gym()
        {
        }

        public Gym(PokemonFortProto fortData, ulong cellId)
        {
            Id = fortData.FortId;
            Latitude = fortData.Latitude;
            Longitude = fortData.Longitude;
            Enabled = fortData.Enabled;
            GuardingPokemonId = Convert.ToUInt16(fortData.GuardPokemonId);
            Team = fortData.Team;
            AvailableSlots = Convert.ToUInt16(fortData.GymDisplay.SlotsAvailable);
            LastModifiedTimestamp = Convert.ToUInt64(fortData.LastModifiedMs / 1000);
            IsExRaidEligible = fortData.IsExRaidEligible;
            InBattle = fortData.IsInBattle;
            IsArScanEligible = fortData.IsArScanEligible;
            var now = DateTime.UtcNow.ToTotalSeconds();
            var powerUpLevelExpirationMs = Convert.ToUInt32(fortData.PowerUpLevelExpirationMs / 1000);
            PowerUpPoints = Convert.ToUInt32(fortData.PowerUpProgressPoints);
            if (fortData.PowerUpProgressPoints < 50)
            {
                PowerUpLevel = 0;
            }
            else if (fortData.PowerUpProgressPoints < 100 && powerUpLevelExpirationMs > now)
            {
                PowerUpLevel = 1;
                PowerUpEndTimestamp = powerUpLevelExpirationMs;
            }
            else if (fortData.PowerUpProgressPoints < 150 && powerUpLevelExpirationMs > now)
            {
                PowerUpLevel = 2;
                PowerUpEndTimestamp = powerUpLevelExpirationMs;
            }
            else if (powerUpLevelExpirationMs > now)
            {
                PowerUpLevel = 3;
                PowerUpEndTimestamp = powerUpLevelExpirationMs;
            }
            else
            {
                PowerUpLevel = 0;
            }

            //PartnerId = fortData.PartnerId != "" ? fortData.PartnerId : null;
            if (fortData.Sponsor != FortSponsor.Types.Sponsor.Unset)
            {
                SponsorId = Convert.ToUInt16(fortData.Sponsor);
            }
            if (!string.IsNullOrEmpty(fortData.ImageUrl))
            {
                Url = fortData.ImageUrl;
            }

            TotalCP = fortData.Team == Team.Unset
                ? 0
                : Convert.ToInt32(fortData.GymDisplay.TotalGymCp);

            if (fortData.RaidInfo != null)
            {
                RaidEndTimestamp = Convert.ToUInt64(fortData.RaidInfo.RaidEndMs / 1000);
                RaidSpawnTimestamp = Convert.ToUInt64(fortData.RaidInfo.RaidSpawnMs / 1000);
                RaidBattleTimestamp = Convert.ToUInt64(fortData.RaidInfo.RaidBattleMs / 1000);
                RaidLevel = Convert.ToUInt16(fortData.RaidInfo.RaidLevel);
                RaidIsExclusive = fortData.RaidInfo.IsExclusive;
                if (fortData.RaidInfo.RaidPokemon != null)
                {
                    RaidPokemonId = Convert.ToUInt16(fortData.RaidInfo.RaidPokemon.PokemonId);
                    RaidPokemonMove1 = Convert.ToUInt16(fortData.RaidInfo.RaidPokemon.Move1);
                    RaidPokemonMove2 = Convert.ToUInt16(fortData.RaidInfo.RaidPokemon.Move2);
                    RaidPokemonForm = Convert.ToUInt16(fortData.RaidInfo.RaidPokemon.PokemonDisplay.Form);
                    RaidPokemonCP = Convert.ToUInt16(fortData.RaidInfo.RaidPokemon.Cp);
                    RaidPokemonGender = Convert.ToUInt16(fortData.RaidInfo.RaidPokemon.PokemonDisplay.Gender);
                    RaidPokemonCostume = Convert.ToUInt16(fortData.RaidInfo.RaidPokemon.PokemonDisplay.Costume);
                    RaidPokemonEvolution = Convert.ToUInt16(fortData.RaidInfo.RaidPokemon.PokemonDisplay.CurrentTempEvolution);
                }
            }

            CellId = cellId;
        }

        public void AddDetails(FortDetailsOutProto fortData)
        {
            Id = fortData.Id;
            Latitude = fortData.Latitude;
            Longitude = fortData.Longitude;
            if ((fortData.ImageUrl?.Count ?? 0) > 0)
            {
                var url = fortData.ImageUrl?.FirstOrDefault();
                if (Url != url)
                {
                    Url = url;
                    HasChanges = true;
                }
            }
            var name = fortData.Name;
            if (Name != name)
            {
                Name = name;
                HasChanges = true;
            }
        }

        public void AddDetails(GymGetInfoOutProto gymInfo)
        {
            var name = gymInfo.Name;
            var url = gymInfo.Url;
            if (Url != url || Name != name)
            {
                Name = name;
                Url = url;
                HasChanges = true;
            }
        }

        public async Task UpdateAsync(MapDataContext context)
        {
            Gym? oldGym = null;
            try
            {
                oldGym = await context.Gyms.FindAsync(Id);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Gym: {ex}");
            }

            if (RaidIsExclusive != null && (RaidIsExclusive ?? false) && ExRaidBossId > 0)
            {
                RaidPokemonId = ExRaidBossId;
                RaidPokemonForm = ExRaidBossForm;
            }

            var now = DateTime.UtcNow.ToTotalSeconds();
            Updated = now;

            context.Attach(this);

            if (oldGym != null)
            {
                if (oldGym.CellId > 0 && CellId == 0)
                {
                    CellId = oldGym.CellId;
                    context.Entry(this).Property(p => p.CellId).IsModified = true;
                }
                if (oldGym.Name != null && Name == null)
                {
                    Name = oldGym.Name;
                    context.Entry(this).Property(p => p.Name).IsModified = true;
                }
                if (oldGym.Url != null && Url == null)
                {
                    Url = oldGym.Url;
                    context.Entry(this).Property(p => p.Url).IsModified = true;
                }
                if (oldGym.RaidIsExclusive != null && RaidIsExclusive == null)
                {
                    RaidIsExclusive = oldGym.RaidIsExclusive;
                    context.Entry(this).Property(p => p.RaidIsExclusive).IsModified = true;
                }
                if (RaidEndTimestamp == null && oldGym.RaidEndTimestamp != null)
                {
                    RaidEndTimestamp = oldGym.RaidEndTimestamp;
                    context.Entry(this).Property(p => p.RaidEndTimestamp).IsModified = true;
                }
                if (RaidBattleTimestamp == null && oldGym.RaidBattleTimestamp != null)
                {
                    RaidBattleTimestamp = oldGym.RaidBattleTimestamp;
                    context.Entry(this).Property(p => p.RaidBattleTimestamp).IsModified = true;
                }
                if (RaidSpawnTimestamp == null && oldGym.RaidSpawnTimestamp != null)
                {
                    RaidSpawnTimestamp = oldGym.RaidSpawnTimestamp;
                    context.Entry(this).Property(p => p.RaidSpawnTimestamp).IsModified = true;
                }
                if (RaidLevel == null && oldGym.RaidLevel != null)
                {
                    RaidLevel = oldGym.RaidLevel;
                    context.Entry(this).Property(p => p.RaidLevel).IsModified = true;
                }
                if (RaidPokemonId == null && oldGym.RaidPokemonId != null)
                {
                    RaidPokemonId = oldGym.RaidPokemonId;
                    context.Entry(this).Property(p => p.RaidPokemonId).IsModified = true;
                }
                if (RaidPokemonForm == null && oldGym.RaidPokemonForm != null)
                {
                    RaidPokemonForm = oldGym.RaidPokemonForm;
                    context.Entry(this).Property(p => p.RaidPokemonForm).IsModified = true;
                }
                if (RaidPokemonCostume == null && oldGym.RaidPokemonCostume != null)
                {
                    RaidPokemonCostume = oldGym.RaidPokemonCostume;
                    context.Entry(this).Property(p => p.RaidPokemonCostume).IsModified = true;
                }
                if (RaidPokemonGender == null && oldGym.RaidPokemonGender != null)
                {
                    RaidPokemonGender = oldGym.RaidPokemonGender;
                    context.Entry(this).Property(p => p.RaidPokemonGender).IsModified = true;
                }
                if (RaidPokemonEvolution == null && oldGym.RaidPokemonEvolution != null)
                {
                    RaidPokemonEvolution = oldGym.RaidPokemonEvolution;
                    context.Entry(this).Property(p => p.RaidPokemonEvolution).IsModified = true;
                }
                if (PowerUpEndTimestamp == null && oldGym.PowerUpEndTimestamp != null)
                {
                    PowerUpEndTimestamp = oldGym.PowerUpEndTimestamp;
                    context.Entry(this).Property(p => p.PowerUpEndTimestamp).IsModified = true;
                }
                if (PowerUpLevel == null && oldGym.PowerUpLevel != null)
                {
                    PowerUpLevel = oldGym.PowerUpLevel;
                    context.Entry(this).Property(p => p.PowerUpLevel).IsModified = true;
                }
                if (PowerUpPoints == null && oldGym.PowerUpPoints != null)
                {
                    PowerUpPoints = oldGym.PowerUpPoints;
                    context.Entry(this).Property(p => p.PowerUpPoints).IsModified = true;
                }

                // TODO: Check shouldUpdate

                if (RaidSpawnTimestamp != null && RaidSpawnTimestamp != 0 &&
                    (
                        oldGym.RaidLevel != RaidLevel ||
                        oldGym.RaidPokemonId != RaidPokemonId ||
                        oldGym.RaidSpawnTimestamp != RaidSpawnTimestamp
                    ))
                {
                    // TODO: Webhooks
                }
            }
        }
    }
}