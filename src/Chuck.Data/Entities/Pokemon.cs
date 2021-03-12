namespace Chuck.Data.Entities
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Text.Json.Serialization;
    using System.Threading.Tasks;

    using Google.Common.Geometry;
    using POGOProtos.Rpc;

    using Chuck.Data.Interfaces;
    using Chuck.Extensions;

    [Table("pokemon")]
    public class Pokemon : BaseEntity, IAggregateRoot, IWebhook
    {
        public const uint DefaultTimeUnseen = 1200;
        public const uint DefaultTimeReseen = 600;
        public const uint DittoPokemonId = (uint)HoloPokemonId.Ditto;
        public const ushort WeatherBoostMinLevel = 6;
        public const ushort WeatherBoostMinIvStat = 4;

        // TODO: Configurable
        private static readonly List<uint> _dittoDisguises = new List<uint>
        {
            163, 167, 187, 223, 293, 316, 322, 399, 590,
        };
        private bool _hasIvChanges;

        #region Properties

        [
            Column("id"),
            Key,
            DatabaseGenerated(DatabaseGeneratedOption.None),
            JsonPropertyName("id"),
        ]
        public ulong Id { get; set; }

        [
            Column("pokestop_id"),
            JsonPropertyName("pokestop_id"),
        ]
        public string PokestopId { get; set; }

        [
            Column("spawn_id"),
            JsonPropertyName("spawn_id"),
        ]
        public ulong? SpawnId { get; set; }

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
            Column("weight"),
            JsonPropertyName("weight"),
        ]
        public double? Weight { get; set; }

        [
            Column("size"),
            JsonPropertyName("size"),
        ]
        public double? Size { get; set; }

        [
            Column("expire_timestamp"),
            JsonPropertyName("expire_timestamp"),
        ]
        public ulong ExpireTimestamp { get; set; }

        [
            Column("updated"),
            DefaultValue(1),
            JsonPropertyName("updated"),
        ]
        public ulong Updated { get; set; }

        [
            Column("pokemon_id"),
            JsonPropertyName("pokemon_id"),
        ]
        public uint PokemonId { get; set; }

        [
            Column("move_1"),
            JsonPropertyName("move_1"),
        ]
        public uint? Move1 { get; set; }

        [
            Column("move_2"),
            JsonPropertyName("move_2"),
        ]
        public uint? Move2 { get; set; }

        [
            Column("gender"),
            JsonPropertyName("gender"),
        ]
        public ushort Gender { get; set; }

        [
            Column("cp"),
            JsonPropertyName("cp"),
        ]
        public ushort? CP { get; set; }

        [
            Column("atk_iv"),
            JsonPropertyName("atk_iv"),
        ]
        public ushort? AttackIV { get; set; }

        [
            Column("def_iv"),
            JsonPropertyName("def_iv"),
        ]
        public ushort? DefenseIV { get; set; }

        [
            Column("sta_iv"),
            JsonPropertyName("sta_iv"),
        ]
        public ushort? StaminaIV { get; set; }

        [
            Column("form"),
            JsonPropertyName("form"),
        ]
        public ushort? Form { get; set; }

        [
            Column("level"),
            JsonPropertyName("level"),
        ]
        public ushort? Level { get; set; }

        [
            Column("weather"),
            JsonPropertyName("weather"),
        ]
        public ushort Weather { get; set; }

        [
            Column("costume"),
            JsonPropertyName("costume"),
        ]
        public ushort Costume { get; set; }

        [
            Column("first_seen_timestamp"),
            JsonPropertyName("first_seen_timestamp"),
        ]
        public ulong FirstSeenTimestamp { get; set; }

        [
            Column("changed"),
            JsonPropertyName("changed"),
        ]
        public ulong Changed { get; set; }

        [
            Column("iv"),
            JsonPropertyName("iv"),
        ]
        public double? IV { get; } // Virtual column

        [
            Column("cell_id"),
            JsonPropertyName("cell_id"),
        ]
        public ulong CellId { get; set; }

        [
            Column("expire_timestamp_verified"),
            JsonPropertyName("expire_timestamp_verified"),
        ]
        public bool IsExpireTimestampVerified { get; set; }

        [
            Column("shiny"),
            JsonPropertyName("shiny"),
        ]
        public bool? IsShiny { get; set; }

        [
            Column("username"),
            JsonPropertyName("username"),
        ]
        public string Username { get; set; }

        [
            Column("display_pokemon_id"),
            JsonPropertyName("display_pokemon_id"),
        ]
        public uint? DisplayPokemonId { get; set; }

        [
            Column("pvp_rankings_great_league"),
            JsonPropertyName("pvp_rankings_great_league"),
        ]
        public List<dynamic> PvpRankingsGreatLeague { get; set; }

        [
            Column("pvp_rankings_ultra_league"),
            JsonPropertyName("pvp_rankings_ultra_league"),
        ]
        public List<dynamic> PvpRankingsUltraLeague { get; set; }

        [
            Column("is_event"),
            JsonPropertyName("is_event"),
        ]
        public bool IsEvent { get; set; }

        [
            NotMapped,
            JsonIgnore,
        ]
        public bool IsDitto { get; private set; }

        [
            NotMapped,
            JsonIgnore,
        ]
        public bool HasChanges { get; private set; }

        [
            NotMapped,
            JsonIgnore,
        ]
        public bool NoWeatherIVClearing { get; }

        [
            NotMapped,
            JsonIgnore,
        ]
        public bool ProcessPvpRankings { get; set; } = true;

        #endregion

        #region Constructor(s)

        public Pokemon()
        {
        }

        public Pokemon(WildPokemonProto wildPokemon, ulong cellId, ulong timestampMs, string username, bool isEvent) : this()
        {
            IsEvent = isEvent;
            Id = wildPokemon.EncounterId;
            PokemonId = (uint)wildPokemon.Pokemon.PokemonId;
            Latitude = wildPokemon.Latitude;
            Longitude = wildPokemon.Longitude;
            var spawnId = Convert.ToUInt64(wildPokemon.SpawnPointId, 16);
            Gender = (ushort)wildPokemon.Pokemon.PokemonDisplay.Gender;
            Form = (ushort?)wildPokemon.Pokemon?.PokemonDisplay?.Form;
            if (wildPokemon.Pokemon.PokemonDisplay != null)
            {
                Costume = (ushort)wildPokemon.Pokemon.PokemonDisplay.Costume;
                Weather = (ushort)wildPokemon.Pokemon.PokemonDisplay.WeatherBoostedCondition;
                // The wildPokemon and nearbyPokemon don't contain actual shininess.
                //IsShiny = wildPokemon.Pokemon.PokemonDisplay.Shiny;
            }
            if (wildPokemon.Pokemon.Cp > 0)
            {
                Weight = wildPokemon.Pokemon.WeightKg;
                Size = wildPokemon.Pokemon.HeightM;
                //ExpireTimestamp = now + 1200;
                PokemonId = (uint)wildPokemon.Pokemon.PokemonId;
                Move1 = (uint?)wildPokemon.Pokemon.Move1;
                Move2 = (uint?)wildPokemon.Pokemon.Move2;
                CP = (ushort?)wildPokemon.Pokemon.Cp;
                AttackIV = (ushort?)wildPokemon.Pokemon.IndividualAttack;
                DefenseIV = (ushort?)wildPokemon.Pokemon.IndividualDefense;
                StaminaIV = (ushort?)wildPokemon.Pokemon.IndividualStamina;
            }

            Username = username;
            var now = DateTime.UtcNow.ToTotalSeconds();
            Changed = now;
            Updated = now;
            //FirstSeenTimestamp = now;

            HandleSpawnpoint(wildPokemon.TimeTillHiddenMs, timestampMs).ConfigureAwait(false)
                                                                       .GetAwaiter()
                                                                       .GetResult();

            SpawnId = spawnId;
            CellId = cellId;
        }

        public Pokemon(NearbyPokemonProto nearbyPokemon, ulong cellId, string username, bool isEvent) : this()
        {
            IsEvent = isEvent;
            Id = nearbyPokemon.EncounterId;
            PokemonId = (uint)nearbyPokemon.PokedexNumber;
            PokestopId = nearbyPokemon.FortId;
             Gender = (ushort)nearbyPokemon.PokemonDisplay.Gender;
            Form = (ushort?)nearbyPokemon.PokemonDisplay?.Form;
            if (nearbyPokemon.PokemonDisplay != null)
            {
                Costume = (ushort)nearbyPokemon.PokemonDisplay.Costume;
                Weather = (ushort)nearbyPokemon.PokemonDisplay.WeatherBoostedCondition;
                // The wildPokemon and nearbyPokemon don't contain actual shininess.
                //IsShiny = wildPokemon.Pokemon.PokemonDisplay.Shiny;
            }
            Username = username;
            //var now = DateTime.UtcNow.ToTotalSeconds();
            //Changed = now;
            //Updated = now;
            //FirstSeenTimestamp = now;

            CellId = cellId;
            IsExpireTimestampVerified = false;
            if (ExpireTimestamp == 0)
            {
                ExpireTimestamp = DateTime.UtcNow.ToTotalSeconds() + DefaultTimeUnseen;
            }
        }

        #endregion

        public PokemonResult Update(Pokemon oldPokemon = null, bool updateIV = false)
        {
            var now = DateTime.UtcNow.ToTotalSeconds();
            var setIVForWeather = false;
            var result = new PokemonResult();

            if (oldPokemon == null)
            {
                Updated = now;
                //FirstSeenTimestamp = now;
                Changed = now;
            }
            else
            {
                /*
                if (oldPokemon.FirstSeenTimestamp == 0 && FirstSeenTimestamp == 0)
                {
                    FirstSeenTimestamp = now;
                }
                else
                {
                    FirstSeenTimestamp = oldPokemon.FirstSeenTimestamp;
                }
                */
                if (ExpireTimestamp == 0)
                {
                    if (oldPokemon.ExpireTimestamp - now < DefaultTimeReseen)
                    {
                        ExpireTimestamp = now + DefaultTimeReseen;
                    }
                    else
                    {
                        ExpireTimestamp = oldPokemon.ExpireTimestamp;
                    }
                }
                if (!IsExpireTimestampVerified && oldPokemon.IsExpireTimestampVerified)
                {
                    IsExpireTimestampVerified = oldPokemon.IsExpireTimestampVerified;
                    ExpireTimestamp = oldPokemon.ExpireTimestamp;
                }
                if (oldPokemon.PokemonId != PokemonId)
                {
                    if (oldPokemon.PokemonId != DittoPokemonId)
                    {
                        ConsoleExt.WriteInfo($"[Pokemon] Pokemon {Id} changed from {oldPokemon.PokemonId} to {PokemonId}");
                    }
                    else if ((oldPokemon.DisplayPokemonId ?? 0) != PokemonId)
                    {
                        ConsoleExt.WriteInfo($"[Pokemon] Pokemon {Id} Ditto disguised as {oldPokemon.DisplayPokemonId} now see as {PokemonId}");
                    }
                }
                if (oldPokemon.CellId > 0 && CellId == 0)
                {
                    CellId = oldPokemon.CellId;
                }
                if (oldPokemon.SpawnId != null)
                {
                    SpawnId = oldPokemon.SpawnId;
                    Latitude = oldPokemon.Latitude;
                    Longitude = oldPokemon.Longitude;
                }
                if (!string.IsNullOrEmpty(oldPokemon.PokestopId) && string.IsNullOrEmpty(PokestopId))
                {
                    PokestopId = oldPokemon.PokestopId;
                }
                if (oldPokemon.PvpRankingsGreatLeague != null && PvpRankingsGreatLeague == null)
                {
                    PvpRankingsGreatLeague = oldPokemon.PvpRankingsGreatLeague;
                }
                if (oldPokemon.PvpRankingsUltraLeague != null && PvpRankingsUltraLeague == null)
                {
                    PvpRankingsUltraLeague = oldPokemon.PvpRankingsUltraLeague;
                }
                if (updateIV && oldPokemon.AttackIV == null && AttackIV != null)
                {
                    Changed = now;
                }
                else
                {
                    Changed = oldPokemon.Changed;
                }
                var weatherChanged = (oldPokemon.Weather == 0 && Weather > 0) || (Weather == 0 && oldPokemon.Weather > 0);
                if (oldPokemon.AttackIV != null && AttackIV == null && !weatherChanged)
                {
                    setIVForWeather = false;
                    AttackIV = oldPokemon.AttackIV;
                    DefenseIV = oldPokemon.DefenseIV;
                    StaminaIV = oldPokemon.StaminaIV;
                    CP = oldPokemon.CP;
                    Weight = oldPokemon.Weight;
                    Size = oldPokemon.Size;
                    Move1 = oldPokemon.Move1;
                    Move2 = oldPokemon.Move2;
                    Level = oldPokemon.Level;
                    IsShiny = oldPokemon.IsShiny;
                    IsDitto = IsDittoDisguised(oldPokemon);
                    if (IsDitto)
                    {
                        ConsoleExt.WriteInfo($"[Pokemon] OldPokemon {Id} Ditto found, disguised as {PokemonId}");
                        SetDittoAttributes(PokemonId);
                    }
                }
                else if ((AttackIV != null && oldPokemon.AttackIV == null) || (CP != null && oldPokemon.CP == null) || _hasIvChanges)
                {
                    setIVForWeather = false;
                    updateIV = true;
                }
                else if (weatherChanged && oldPokemon.AttackIV != null && !NoWeatherIVClearing)
                {
                    ConsoleExt.WriteInfo($"[Pokemon] Pokemon {Id} changed WeatherBoosted state. Clearing IVs");
                    setIVForWeather = true;
                    AttackIV = null;
                    DefenseIV = null;
                    StaminaIV = null;
                    CP = null;
                    Weight = null;
                    Size = null;
                    Move1 = null;
                    Move2 = null;
                    Level = null;
                    PvpRankingsGreatLeague = null;
                    PvpRankingsUltraLeague = null;
                    ConsoleExt.WriteInfo("[Pokemon] WeatherBoosted state changed. Cleared IVs");
                }
                else
                {
                    setIVForWeather = false;
                }

                // Check if we should update the pokemon or not
                if (!ShouldUpdate(oldPokemon, this))
                    return result;

                if (oldPokemon.PokemonId == DittoPokemonId && PokemonId != DittoPokemonId)
                {
                    ConsoleExt.WriteInfo($"[Pokemon] Pokemon {Id} Ditto changed from {oldPokemon.PokemonId} to {PokemonId}");
                }

                Updated = now;
            }

            if (setIVForWeather)
            {
                result.IsNewOrHasChanges = true;
                result.Webhook = true;
            }
            else if (oldPokemon == null)
            {
                result.IsNewOrHasChanges = true;
                result.Webhook = true;
                if (AttackIV != null)
                {
                    result.GotIV = true;
                }
            }
            else if ((updateIV && oldPokemon.AttackIV == null && AttackIV != null) || oldPokemon._hasIvChanges)
            {
                oldPokemon._hasIvChanges = false;
                result.Webhook = true;
                result.GotIV = true;
            }
            return result;
        }

        public static bool ShouldUpdate(Pokemon oldPokemon, Pokemon newPokemon)
        {
            if (oldPokemon.HasChanges)
            {
                oldPokemon.HasChanges = false;
                return true;
            }
            return
                newPokemon.PokemonId != oldPokemon.PokemonId ||
                newPokemon.SpawnId != oldPokemon.SpawnId ||
                newPokemon.PokestopId != oldPokemon.PokestopId ||
                newPokemon.Weather != oldPokemon.Weather ||
                newPokemon.ExpireTimestamp != oldPokemon.ExpireTimestamp ||
                newPokemon.AttackIV != oldPokemon.AttackIV ||
                newPokemon.DefenseIV != oldPokemon.DefenseIV ||
                newPokemon.StaminaIV != oldPokemon.StaminaIV ||
                newPokemon.CP != oldPokemon.CP ||
                newPokemon.Level != oldPokemon.Level ||
                newPokemon.Move1 != oldPokemon.Move1 ||
                newPokemon.Move2 != oldPokemon.Move2 ||
                newPokemon.Gender != oldPokemon.Gender ||
                newPokemon.Form != oldPokemon.Form ||
                newPokemon.Costume != oldPokemon.Costume ||
                Math.Abs((double)newPokemon.ExpireTimestamp) - oldPokemon.ExpireTimestamp > 60 ||
                Math.Abs(newPokemon.Latitude - oldPokemon.Latitude) >= 0.000001 ||
                Math.Abs(newPokemon.Longitude - oldPokemon.Longitude) >= 0.000001;
        }

        #region Ditto

        public static bool IsDittoDisguised(Pokemon pokemon) => IsDittoDisguised(
                pokemon.PokemonId,
                pokemon.Level ?? 0,
                pokemon.Weather,
                pokemon.AttackIV ?? 0,
                pokemon.DefenseIV ?? 0,
                pokemon.StaminaIV ?? 0
        );

        public static bool IsDittoDisguised(uint pokemonId, ushort level, ushort weather, ushort atkIv, ushort defIv, ushort staIv)
        {
            var isDisguised = pokemonId == DittoPokemonId || _dittoDisguises.Contains(pokemonId);
            var isUnderLevelBoosted = level > 0 && level < WeatherBoostMinLevel;
            var isUnderIvStatBoosted = level > 0 &&
                (atkIv < WeatherBoostMinIvStat ||
                defIv < WeatherBoostMinIvStat ||
                staIv < WeatherBoostMinIvStat);
            var isWeatherBoosted = weather > 0;
            return isDisguised && (isUnderLevelBoosted || isUnderIvStatBoosted) && isWeatherBoosted;
        }

        private void SetDittoAttributes(uint displayPokemonId)
        {
            const HoloPokemonMove moveTransfromFast = HoloPokemonMove.TransformFast;
            const HoloPokemonId moveStruggle = HoloPokemonId.Ditto;
            DisplayPokemonId = displayPokemonId;
            PokemonId = DittoPokemonId;
            Form = 0;
            Move1 = (uint)moveTransfromFast;
            Move2 = (uint)moveStruggle;
            Gender = 3;
            Costume = 0;
            Size = 0;
            Weight = 0;
        }

        #endregion

        public async Task AddEncounter(EncounterOutProto encounter, string username)
        {
            var pokemonId = (uint)encounter.Pokemon.Pokemon.PokemonId;
            var cp = (ushort?)encounter.Pokemon.Pokemon.Cp;
            var move1 = (uint?)encounter.Pokemon.Pokemon.Move1;
            var move2 = (uint?)encounter.Pokemon.Pokemon.Move2;
            var size = (double?)encounter.Pokemon.Pokemon.HeightM;
            var weight = (double?)encounter.Pokemon.Pokemon.WeightKg;
            var atkIV = (ushort?)encounter.Pokemon.Pokemon.IndividualAttack;
            var defIV = (ushort?)encounter.Pokemon.Pokemon.IndividualDefense;
            var staIV = (ushort?)encounter.Pokemon.Pokemon.IndividualStamina;
            var costume = (ushort)encounter.Pokemon.Pokemon.PokemonDisplay.Costume;
            var form = (ushort?)encounter.Pokemon.Pokemon.PokemonDisplay.Form;
            var gender = (ushort)encounter.Pokemon.Pokemon.PokemonDisplay.Gender;
            if (PokemonId != pokemonId ||
                CP != cp ||
                Move1 != move1 ||
                Move2 != move2 ||
                Size != size ||
                Weight != weight ||
                AttackIV != atkIV ||
                DefenseIV != defIV ||
                StaminaIV != staIV ||
                Costume != costume ||
                Form != form ||
                Gender != gender)
            {
                HasChanges = true;
                _hasIvChanges = true;
            }

            PokemonId = pokemonId;
            CP = cp;
            Move1 = move1;
            Move2 = move2;
            Size = size;
            Weight = weight;
            AttackIV = atkIV;
            DefenseIV = defIV;
            StaminaIV = staIV;
            Costume = costume;
            Form = form;
            Gender = gender;
            IsShiny = encounter.Pokemon.Pokemon.PokemonDisplay.Shiny;
            Username = username;

            if (_hasIvChanges)
            {
                var cpMultiplier = encounter.Pokemon.Pokemon.CpMultiplier;
                ushort level;
                if (cpMultiplier < 0.734)
                {
                    level = (ushort)Math.Round(
                        (58.35178527 * cpMultiplier * cpMultiplier) -
                        (2.838007664 * cpMultiplier) + 0.8539209906
                    );
                }
                else
                {
                    level = (ushort)Math.Round((171.0112688 * cpMultiplier) - 95.20425243);
                }
                Level = level;
                IsDitto = IsDittoDisguised(pokemonId, level, Weather, AttackIV ?? 0, DefenseIV ?? 0, StaminaIV ?? 0);
                if (IsDitto)
                {
                    ConsoleExt.WriteInfo($"[Pokemon] Pokemon {Id} Ditto found, disguised as {PokemonId}");
                    SetDittoAttributes(PokemonId);
                }

                if (AttackIV != null)
                {
                    await SetPvpRankings().ConfigureAwait(false);
                }

                SpawnId = Convert.ToUInt64(encounter.Pokemon.SpawnPointId, 16);
                var timestampMs = DateTime.UtcNow.ToTotalSeconds();
                await HandleSpawnpoint(encounter.Pokemon.TimeTillHiddenMs, timestampMs).ConfigureAwait(false);

                Updated = DateTime.UtcNow.ToTotalSeconds();
                Changed = Updated;
                if (FirstSeenTimestamp == 0)
                {
                    FirstSeenTimestamp = Updated;
                }
            }
            await Task.CompletedTask.ConfigureAwait(false);
        }

        public async Task<Spawnpoint> HandleSpawnpoint(int timeTillHiddenMs, ulong timestampMs)
        {
            var now = DateTime.UtcNow.ToTotalSeconds();
            if (timeTillHiddenMs <= 90000 && timeTillHiddenMs > 0)
            {
                ExpireTimestamp = Convert.ToUInt64(timestampMs + Convert.ToDouble(timeTillHiddenMs)) / 1000;
                IsExpireTimestampVerified = true;
                var unixDate = timestampMs.FromMilliseconds();
                var secondOfHour = unixDate.Second + (unixDate.Minute * 60);
                return new Spawnpoint
                {
                    Id = SpawnId ?? 0,
                    Latitude = Latitude,
                    Longitude = Longitude,
                    Updated = now,
                    DespawnSecond = (ushort)secondOfHour,
                    FirstSeenTimestamp = now,
                };
            }
            else
            {
                IsExpireTimestampVerified = false;
            }

            if (!IsExpireTimestampVerified && SpawnId != null)
            {
                Spawnpoint spawnpoint = null;
                try
                {
                    if (SpawnId != null)
                    {
                        // spawnpoint = await _spawnpointRepository.GetByIdAsync(SpawnId ?? 0).ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    ConsoleExt.WriteError($"[Pokemon] Error: {ex}");
                    spawnpoint = null;
                }
                if (spawnpoint != null && spawnpoint?.DespawnSecond != null)
                {
                    var unixDate = timestampMs.FromMilliseconds();
                    var secondOfHour = unixDate.Second + (unixDate.Minute * 60);
                    var despawnOffset = spawnpoint.DespawnSecond - secondOfHour;
                    if (spawnpoint.DespawnSecond < secondOfHour)
                        despawnOffset += 3600;
                    ExpireTimestamp = now + (ulong)(despawnOffset ?? 0);
                    IsExpireTimestampVerified = true;
                }
                else
                {
                    spawnpoint = new Spawnpoint
                    {
                        Id = SpawnId ?? 0,
                        Latitude = Latitude,
                        Longitude = Longitude,
                        Updated = now,
                        DespawnSecond = null,
                        FirstSeenTimestamp = now,
                    };
                }
                return await Task.FromResult(spawnpoint).ConfigureAwait(false);
            }

            if (ExpireTimestamp == 0)
            {
                //Console.WriteLine($"[Pokemon] ExpireTimestamp == 0");
                ExpireTimestamp = DateTime.UtcNow.ToTotalSeconds();
                IsExpireTimestampVerified = false;
            }
            return null;
        }

        public static ulong CalculateDespawnTimer(ushort? despawnSecond)
        {
            var unixDate = DateTime.UtcNow;
            var now = unixDate.ToTotalSeconds();
            var secondOfHour = unixDate.Second + (unixDate.Minute * 60);
            var despawnOffset = despawnSecond - secondOfHour;
            if (despawnOffset < 0)
            {
                despawnOffset += 3600;
            }
            return now + (ulong)despawnOffset;
        }

        public dynamic GetWebhookValues(string type)
        {
            return new
            {
                type  = "pokemon",
                message = new
                {
                    spawnpoint_id = SpawnId?.ToString("X") ?? "None",
                    pokestop_id = PokestopId ?? "None",
                    encounter_id = Id,
                    pokemon_id = PokemonId,
                    latitude = Latitude,
                    longitude = Longitude,
                    disappear_time = ExpireTimestamp,
                    disappear_time_verified = IsExpireTimestampVerified,
                    first_seen = FirstSeenTimestamp,
                    last_modified_time = Updated,
                    gender = Gender,
                    cp = CP,
                    form = Form,
                    costume = Costume,
                    individual_attack = AttackIV,
                    individual_defense = DefenseIV,
                    individual_stamina = StaminaIV,
                    pokemon_level = Level,
                    move_1 = Move1,
                    move_2 = Move2,
                    weight = Weight,
                    height = Size,
                    weather = Weather,
                    shiny = IsShiny,
                    username = Username,
                    display_pokemon_id = DisplayPokemonId,
                    pvp_rankings_great_league = PvpRankingsGreatLeague,
                    pvp_rankings_ultra_league = PvpRankingsUltraLeague,
                    is_event = IsEvent,
                },
            };
        }

        private Task SetPvpRankings()
        {
            if (!ProcessPvpRankings)
                return Task.CompletedTask;

            return Task.Run(() =>
            {
                // TODO: Set Pokemon PVP stats
                /*
                var ranks = PvpRankCalculator.Instance.QueryPvpRank
                (
                    PokemonId,
                    Form ?? 0,
                    Costume,
                    AttackIV ?? 0,
                    DefenseIV ?? 0,
                    StaminaIV ?? 0,
                    Level ?? 0,
                    (PokemonGender)Gender
                );
                if (ranks.Count == 0)
                    return;

                if (ranks.ContainsKey("great"))
                {
                    PvpRankingsGreatLeague = ranks["great"];
                }
                if (ranks.ContainsKey("ultra"))
                {
                    PvpRankingsUltraLeague = ranks["ultra"];
                }
                */
            });
        }
    }

    public class PokemonResult
    {
        public bool IsNewOrHasChanges { get; set; }

        public bool GotIV { get; set; }

        public bool Webhook { get; set; }
    }
}