namespace Chuck.Data.Entities
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Linq;
    using System.Text.Json.Serialization;

    using POGOProtos.Rpc;

    using Chuck.Data.Interfaces;
    using Chuck.Extensions;
    //using Chuck.Infrastructure.Net.Webhooks;

    [Table("gym")]
    public class Gym : BaseEntity, IAggregateRoot, IWebhook
    {
        [
            Column("id"),
            Key,
            DatabaseGenerated(DatabaseGeneratedOption.None),
            JsonPropertyName("id"),
        ]
        public string Id { get; set; }

        [
            Column("lat"),
            JsonPropertyName("lat"),
        ]
        public double Latitude { get; set; }

        [
            Column("lon"),
            JsonPropertyName("lon"),
        ]
        public double Longitude { get; set; }

        [
            Column("name"),
            JsonPropertyName("name"),
        ]
        public string Name { get; set; }

        [
            Column("url"),
            JsonPropertyName("url"),
        ]
        public string Url { get; set; }

        [
            Column("last_modified_timestamp"),
            JsonPropertyName("last_modified_timestamp"),
        ]
        public ulong LastModifiedTimestamp { get; set; }

        [
            Column("raid_end_timestamp"),
            JsonPropertyName("raid_end_timestamp"),
        ]
        public ulong? RaidEndTimestamp { get; set; }

        [
            Column("raid_spawn_timestamp"),
            JsonPropertyName("raid_spawn_timestamp"),
        ]
        public ulong? RaidSpawnTimestamp { get; set; }

        [
            Column("raid_battle_timestamp"),
            JsonPropertyName("raid_battle_timestamp"),
        ]
        public ulong? RaidBattleTimestamp { get; set; }

        [
            Column("updated"),
            JsonPropertyName("updated"),
        ]
        public ulong Updated { get; set; }

        [
            Column("raid_pokemon_id"),
            JsonPropertyName("raid_pokemon_id"),
        ]
        public uint? RaidPokemonId { get; set; }

        [
            Column("guarding_pokemon_id"),
            JsonPropertyName("guarding_pokemon_id"),
        ]
        public uint GuardingPokemonId { get; set; }

        [
            Column("availble_slots"), // I cringe every time
            JsonPropertyName("available_slots"),
        ] 
        public ushort AvailableSlots { get; set; }

        [
            Column("team_id"),
            JsonPropertyName("team_id"),
        ]
        public Team Team { get; set; }

        [
            Column("raid_level"),
            JsonPropertyName("raid_level"),
        ]
        public ushort RaidLevel { get; set; }

        [
            Column("enabled"),
            JsonPropertyName("enabled"),
        ]
        public bool Enabled { get; set; }

        [
            Column("ex_raid_eligible"),
            JsonPropertyName("ex_raid_eligible"),
        ]
        public bool ExRaidEligible { get; set; }

        [
            Column("in_battle"),
            JsonPropertyName("in_battle"),
        ]
        public bool InBattle { get; set; }

        [
            Column("raid_pokemon_move_1"),
            JsonPropertyName("raid_pokemon_move_1"),
        ]
        public uint? RaidPokemonMove1 { get; set; }

        [
            Column("raid_pokemon_move_2"),
            JsonPropertyName("raid_pokemon_move_2"),
        ]
        public uint? RaidPokemonMove2 { get; set; }

        [
            Column("raid_pokemon_form"),
            JsonPropertyName("raid_pokemon_form"),
        ]
        public uint? RaidPokemonForm { get; set; }

        [
            Column("raid_pokemon_costume"),
            JsonPropertyName("raid_pokemon_costume"),
        ]
        public uint RaidPokemonCostume { get; set; }

        [
            Column("raid_pokemon_evolution"),
            JsonPropertyName("raid_pokemon_evolution"),
        ]
        public uint RaidPokemonEvolution { get; set; }

        [
            Column("raid_pokemon_gender"),
            JsonPropertyName("raid_pokemon_gender"),
        ]
        public ushort? RaidPokemonGender { get; set; }

        [
            Column("raid_pokemon_cp"),
            JsonPropertyName("raid_pokemon_cp"),
        ]
        public uint? RaidPokemonCP { get; set; }

        [
            Column("raid_is_exclusive"),
            JsonPropertyName("raid_is_exclusive"),
        ]
        public bool RaidIsExclusive { get; set; }

        [
            Column("cell_id"),
            JsonPropertyName("cell_id"),
        ]
        public ulong CellId { get; set; }

        [
            Column("deleted"),
            JsonPropertyName("deleted"),
        ]
        public bool Deleted { get; set; }

        [
            Column("total_cp"),
            JsonPropertyName("total_cp"),
        ]
        public int TotalCP { get; set; }

        [
            Column("first_seen_timestamp"),
            JsonPropertyName("first_seen_timestamp"),
        ]
        public ulong FirstSeenTimestamp { get; set; }

        [
            Column("sponsor_id"),
            JsonPropertyName("sponsor_id"),
        ]
        public uint SponsorId { get; set; }

        public Gym()
        {
        }

        public Gym(ulong cellId, PokemonFortProto fort)
        {
            var now = DateTime.UtcNow.ToTotalSeconds();
            Id = fort.FortId;
            Latitude = fort.Latitude;
            Longitude = fort.Longitude;
            //Name = fort.Name;
            Url = fort.ImageUrl;
            GuardingPokemonId = (uint)fort.GuardPokemonId;
            AvailableSlots = (ushort?)fort.GymDisplay?.SlotsAvailable ?? 0;
            LastModifiedTimestamp = (ulong)fort.LastModifiedMs / 1000;
            Team = fort.Team;
            Enabled = fort.Enabled;
            ExRaidEligible = fort.IsExRaidEligible;
            InBattle = fort.IsInBattle;
            TotalCP = fort.GymDisplay.TotalGymCp;
            CellId = cellId;
            SponsorId = (uint)fort.Sponsor;
            Updated = now;
            Deleted = false;
            //IsArScanEligible = fort.IsArScanEligible;
            if (fort.RaidInfo != null)
            {
                Url = fort.ImageUrl;
                RaidEndTimestamp = Convert.ToUInt64(fort.RaidInfo.RaidEndMs / 1000);
                RaidSpawnTimestamp = Convert.ToUInt64(fort.RaidInfo.RaidSpawnMs / 1000);
                RaidBattleTimestamp = Convert.ToUInt64(fort.RaidInfo.RaidBattleMs / 1000);
                RaidLevel = (ushort)fort.RaidInfo.RaidLevel;
                RaidIsExclusive = fort.RaidInfo.IsExclusive;
                if (fort.RaidInfo.RaidPokemon != null)
                {
                    RaidPokemonId = (uint?)fort.RaidInfo.RaidPokemon.PokemonId;
                    RaidPokemonMove1 = (uint?)fort.RaidInfo.RaidPokemon.Move1;
                    RaidPokemonMove2 = (uint?)fort.RaidInfo.RaidPokemon.Move2;
                    RaidPokemonCP = (uint?)fort.RaidInfo.RaidPokemon.Cp;
                    RaidPokemonForm = (uint?)fort.RaidInfo.RaidPokemon.PokemonDisplay.Form;
                    RaidPokemonGender = (ushort)fort.RaidInfo.RaidPokemon.PokemonDisplay.Gender;
                    RaidPokemonEvolution = (uint)fort.RaidInfo.RaidPokemon.PokemonDisplay.CurrentTempEvolution;
                }
            }
        }

        // TODO: Add ShouldUpdate method

        public GymResult Update(Gym oldGym = null)
        {
            var now = DateTime.UtcNow.ToTotalSeconds();
            Updated = now;
            var result = new GymResult();
            if (oldGym == null)
            {
                result.IsNewOrHasChanges = true;
                result.SendGym = true;
                result.SendGymInfo = true;
                // WebhookController.Instance.AddGym(this);
                // WebhookController.Instance.AddGymInfo(this);

                var raidBattleTime = now - RaidBattleTimestamp;
                var raidEndTime = now - RaidEndTimestamp;
                if (raidBattleTime > now && RaidLevel > 0)
                {
                    // WebhookController.Instance.AddRaid(this);
                    result.SendRaid = true;
                }
                else if (raidEndTime > now && RaidPokemonId > 0)
                {
                    // WebhookController.Instance.AddRaid(this);
                    result.SendRaid = true;
                }
            }
            else
            {
                if (RaidSpawnTimestamp > 0 &&
                    (
                    oldGym.RaidLevel != RaidLevel ||
                    oldGym.RaidPokemonId != RaidPokemonId ||
                    oldGym.RaidSpawnTimestamp != RaidSpawnTimestamp))
                {
                    var raidBattleTime = now - RaidBattleTimestamp;
                    var raidEndTime = now - RaidEndTimestamp;
                    if (raidBattleTime > now && RaidLevel > 0)
                    {
                        // WebhookController.Instance.AddEgg(this);
                        result.SendEgg = true;
                    }
                    else if (raidEndTime > now && RaidPokemonId > 0)
                    {
                        // WebhookController.Instance.AddRaid(this);
                        result.SendRaid = true;
                    }
                }
                if (oldGym.AvailableSlots != AvailableSlots ||
                    oldGym.Team != Team ||
                    oldGym.InBattle != InBattle)
                {
                    // WebhookController.Instance.AddGymInfo(this);
                    result.SendGymInfo = true;
                }
            }
            return result;
        }

        public void AddDetails(FortDetailsOutProto fortDetails)
        {
            Id = fortDetails.Id;
            Latitude = fortDetails.Latitude;
            Longitude = fortDetails.Longitude;
            if (string.Compare(Name, fortDetails.Name, true) != 0)
            {
                // HasChanges = true;
                Name = fortDetails.Name;
            }
            if (fortDetails.ImageUrl.Count > 0)
            {
                var url = fortDetails.ImageUrl.FirstOrDefault();
                // Check if url changed
                if (string.Compare(Url, url, true) != 0)
                {
                    // HasChanges = true
                    Url = url;
                }
            }
            Updated = DateTime.UtcNow.ToTotalSeconds();
        }

        public void AddDetails(GymGetInfoOutProto gymDetails)
        {
            // Check if gym details name is null or empty
            if (!string.IsNullOrEmpty(gymDetails.Name))
            {
                // Check if current gym name and gym details gym name are different
                if (string.Compare(Name, gymDetails.Name, true) != 0)
                {
                    // If so assign new name
                    Name = gymDetails.Name;
                }
            }
            // Check if gym details url is null or empty
            if (!string.IsNullOrEmpty(gymDetails.Url))
            {
                // Check if current gym url and gym details gym url are different
                if (string.Compare(Url, gymDetails.Url, true) != 0)
                {
                    // If so assign new url
                    Url = gymDetails.Url;
                }
            }
            Updated = DateTime.UtcNow.ToTotalSeconds();
        }

        public dynamic GetWebhookValues(string type)
        {
            dynamic data;
            string realType;
            switch (type.ToLower())
            {
                case "gym":
                    realType = "gym";
                    data = new
                    {
                        gym_id = Id,
                        gym_name = string.IsNullOrEmpty(Name) ? "Unknown" : Name,
                        latitude = Latitude,
                        longitude = Longitude,
                        url = Url,
                        enabled = Enabled,
                        team_id = (ushort)Team,
                        last_modified = LastModifiedTimestamp,
                        guard_pokemon_id = GuardingPokemonId,
                        slots_available = AvailableSlots,
                        raid_active_until = RaidEndTimestamp ?? 0,
                        ex_raid_eligible = ExRaidEligible,
                        sponsor_id = SponsorId,
                    };
                    break;
                case "gym-info":
                    realType = "gym_details";
                    data = new
                    {
                        id = Id,
                        name = string.IsNullOrEmpty(Name) ? "Unknown" : Name,
                        url = Url,
                        latitude = Latitude,
                        longitude = Longitude,
                        team = (ushort)Team,
                        slots_available = AvailableSlots,
                        ex_raid_eligible = ExRaidEligible,
                        in_battle = InBattle,
                        sponsor_id = SponsorId,
                    };
                    break;
                case "egg":
                case "raid":
                    realType = "raid";
                    data = new
                    {
                        gym_id = Id,
                        gym_name = string.IsNullOrEmpty(Name) ? "Unknown" : Name,
                        gym_url = Url,
                        latitude = Latitude,
                        longitude = Longitude,
                        team_id = (ushort)Team,
                        spawn = RaidSpawnTimestamp ?? 0,
                        start = RaidBattleTimestamp ?? 0,
                        end = RaidEndTimestamp ?? 0,
                        level = RaidLevel,
                        pokemon_id = RaidPokemonId ?? 0,
                        cp = RaidPokemonCP ?? 0,
                        gender = RaidPokemonGender ?? 0,
                        form = RaidPokemonForm ?? 0,
                        evolution = RaidPokemonEvolution,
                        move_1 = RaidPokemonMove1 ?? 0,
                        move_2 = RaidPokemonMove2 ?? 0,
                        ex_raid_eligible = ExRaidEligible,
                        is_exclusive = RaidIsExclusive,
                        sponsor_id = SponsorId,
                    };
                    break;
                default:
                    realType = "Unknown";
                    data = new { };
                    break;
            }
            return new
            {
                type = realType,
                message = data,
            };
        }
    }

    public class GymResult
    {
        public bool IsNewOrHasChanges { get; set; }

        public bool SendGym { get; set; }

        public bool SendGymInfo { get; set; }

        public bool SendRaid { get; set; }

        public bool SendEgg { get; set; }
    }
}