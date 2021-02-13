namespace ChuckDeviceController.Data.Entities
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Threading.Tasks;

    using POGOProtos.Rpc;

    using ChuckDeviceController.Data.Factories;
    using ChuckDeviceController.Data.Interfaces;
    using ChuckDeviceController.Data.Repositories;
    using ChuckDeviceController.Extensions;

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
            46, 163, 167, 187, 223, 293, 316, 322, 399, 590,
        };
        private readonly SpawnpointRepository _spawnpointRepository;
        private bool _hasIvChanges;

        #region Properties

        [
            Column("id"),
            Key,
        ]
        public string Id { get; set; }

        [Column("pokestop_id")]
        public string PokestopId { get; set; }

        [Column("spawn_id")]
        public ulong? SpawnId { get; set; }

        [Column("lat")]
        public double Latitude { get; set; }

        [Column("lon")]
        public double Longitude { get; set; }

        [Column("weight")]
        public double? Weight { get; set; }

        [Column("size")]
        public double? Size { get; set; }

        [Column("expire_timestamp")]
        public ulong ExpireTimestamp { get; set; }

        [
            Column("updated"),
            DefaultValue(1),
        ]
        public ulong Updated { get; set; }

        [Column("pokemon_id")]
        public uint PokemonId { get; set; }

        [Column("move_1")]
        public uint? Move1 { get; set; }

        [Column("move_2")]
        public uint? Move2 { get; set; }

        [Column("gender")]
        public ushort Gender { get; set; }

        [Column("cp")]
        public ushort? CP { get; set; }

        [Column("atk_iv")]
        public ushort? AttackIV { get; set; }

        [Column("def_iv")]
        public ushort? DefenseIV { get; set; }

        [Column("sta_iv")]
        public ushort? StaminaIV { get; set; }

        [Column("form")]
        public ushort? Form { get; set; }

        [Column("level")]
        public ushort? Level { get; set; }

        [Column("weather")]
        public ushort Weather { get; set; }

        [Column("costume")]
        public ushort Costume { get; set; }

        [Column("first_seen_timestamp")]
        public ulong FirstSeenTimestamp { get; set; }

        [Column("changed")]
        public ulong Changed { get; set; }

        [Column("iv")]
        public double? IV { get; }

        [Column("cell_id")]
        public ulong CellId { get; set; }

        [Column("expire_timestamp_verified")]
        public bool IsExpireTimestampVerified { get; set; }

        [Column("shiny")]
        public bool? IsShiny { get; set; }

        [Column("username")]
        public string Username { get; set; }

        [Column("display_pokemon_id")]
        public uint? DisplayPokemonId { get; set; }

        [Column("capture_1")]
        public double? CaptureRate1 { get; set; }

        [Column("capture_2")]
        public double? CaptureRate2 { get; set; }

        [Column("capture_3")]
        public double? CaptureRate3 { get; set; }

        [Column("pvp_rankings_great_league")]
        public List<PvpRank> PvpRankingsGreatLeague { get; set; }

        [Column("pvp_rankings_ultra_league")]
        public List<PvpRank> PvpRankingsUltraLeague { get; set; }

        [Column("is_event")]
        public bool IsEvent { get; set; }

        [NotMapped]
        public bool IsDitto { get; private set; }

        [NotMapped]
        public bool HasChanges { get; private set; }

        #endregion

        #region Constructor(s)

        public Pokemon()
        {
            _spawnpointRepository = new SpawnpointRepository(DbContextFactory.CreateDeviceControllerContext(Startup.DbConfig.ToString()));
        }

        public Pokemon(WildPokemonProto wildPokemon, ulong cellId, ulong timestampMs, string username, bool isEvent) : this()
        {
            IsEvent = isEvent;
            Id = wildPokemon.EncounterId.ToString();
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
            Username = username;
            var now = DateTime.UtcNow.ToTotalSeconds();
            Changed = now;
            Updated = now;
            FirstSeenTimestamp = now;

            HandleSpawnpoint(wildPokemon.TimeTillHiddenMs, timestampMs).ConfigureAwait(false)
                                                                       .GetAwaiter()
                                                                       .GetResult();

            SpawnId = spawnId;
            CellId = cellId;
        }

        public Pokemon(NearbyPokemonProto nearbyPokemon, ulong cellId, string username, bool isEvent) : this()
        {
            IsEvent = isEvent;
            var id = nearbyPokemon.EncounterId.ToString();
            var pokemonId = (uint)nearbyPokemon.PokedexNumber;
            var pokestopId = nearbyPokemon.FortId;
            var gender = (ushort)nearbyPokemon.PokemonDisplay.Gender;
            var form = (ushort?)nearbyPokemon.PokemonDisplay?.Form;
            if (nearbyPokemon.PokemonDisplay != null)
            {
                Costume = (ushort)nearbyPokemon.PokemonDisplay.Costume;
                Weather = (ushort)nearbyPokemon.PokemonDisplay.WeatherBoostedCondition;
                // The wildPokemon and nearbyPokemon don't contain actual shininess.
                //IsShiny = wildPokemon.Pokemon.PokemonDisplay.Shiny;
            }
            Username = username;
            var now = DateTime.UtcNow.ToTotalSeconds();
            Changed = now;
            Updated = now;
            FirstSeenTimestamp = now;

            double lat = 0;
            double lon = 0;
            // TODO: Try to get pokestop

            Id = id;
            PokemonId = pokemonId;
            Latitude = lat;
            Longitude = lon;
            PokestopId = pokestopId;
            Gender = gender;
            Form = form;
            CellId = cellId;
            IsExpireTimestampVerified = false;
            if (ExpireTimestamp == 0)
            {
                ExpireTimestamp = DateTime.UtcNow.ToTotalSeconds() + DefaultTimeUnseen;
            }
        }

        #endregion

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
                if (encounter.CaptureProbability != null)
                {
                    CaptureRate1 = Convert.ToDouble(encounter.CaptureProbability.CaptureProbability[0]);
                    CaptureRate2 = Convert.ToDouble(encounter.CaptureProbability.CaptureProbability[1]);
                    CaptureRate3 = Convert.ToDouble(encounter.CaptureProbability.CaptureProbability[2]);
                }
                var cpMultiplier = encounter.Pokemon.Pokemon.CpMultiplier;
                ushort level;
                if (cpMultiplier < 0.734)
                {
                    level = (ushort)Math.Round(
                        58.35178527 * cpMultiplier * cpMultiplier -
                        2.838007664 * cpMultiplier + 0.8539209906
                    );
                }
                else
                {
                    level = (ushort)Math.Round(171.0112688 * cpMultiplier - 95.20425243);
                }
                Level = level;
                IsDitto = IsDittoDisguised(pokemonId, level, Weather, AttackIV ?? 0, DefenseIV ?? 0, StaminaIV ?? 0);
                if (IsDitto)
                {
                    Console.WriteLine($"[Pokemon] Pokemon {Id} Ditto found, disguised as {PokemonId}");
                    SetDittoAttributes(PokemonId);
                }
                // TODO: SetPVP();

                SpawnId = Convert.ToUInt64(encounter.Pokemon.SpawnPointId, 16);
                var timestampMs = DateTime.UtcNow.ToTotalSeconds();
                await HandleSpawnpoint(encounter.Pokemon.TimeTillHiddenMs, timestampMs);

                Updated = DateTime.UtcNow.ToTotalSeconds();
                Changed = Updated;
                if (FirstSeenTimestamp == 0)
                {
                    FirstSeenTimestamp = Updated;
                }
            }
            await Task.CompletedTask;
        }

        private void SetDittoAttributes(uint displayPokemonId)
        {
            var moveTransfromFast = HoloPokemonMove.TransformFast;
            var moveStruggle = HoloPokemonId.Ditto;
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

        public async Task<Spawnpoint> HandleSpawnpoint(int timeTillHiddenMs, ulong timestampMs)
        {
            if (timeTillHiddenMs <= 90000 && timeTillHiddenMs > 0)
            {
                ExpireTimestamp = Convert.ToUInt64(timestampMs + Convert.ToDouble(timeTillHiddenMs)) / 1000;
                IsExpireTimestampVerified = true;
                var unixDate = timestampMs.FromMilliseconds();
                var secondOfHour = unixDate.Second + unixDate.Minute * 60;
                var spawnpoint = new Spawnpoint
                {
                    Id = SpawnId ?? 0,
                    Latitude = Latitude,
                    Longitude = Longitude,
                    Updated = Updated,
                    DespawnSecond = (ushort)secondOfHour,
                };
                return spawnpoint;
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
                        spawnpoint = await _spawnpointRepository.GetByIdAsync(SpawnId ?? 0);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Pokemon] Error: {ex}");
                    spawnpoint = null;
                }
                if (spawnpoint != null && spawnpoint?.DespawnSecond != null)
                {
                    var unixDate = timestampMs.FromMilliseconds();
                    var secondOfHour = unixDate.Second + unixDate.Minute * 60;
                    ushort despawnOffset;
                    if (spawnpoint.DespawnSecond < secondOfHour)
                    {
                        despawnOffset = Convert.ToUInt16(3600 + spawnpoint.DespawnSecond - secondOfHour);
                    }
                    else
                    {
                        despawnOffset = Convert.ToUInt16(spawnpoint.DespawnSecond - secondOfHour);
                    }
                    var now = DateTime.UtcNow.ToTotalSeconds();
                    ExpireTimestamp = now + despawnOffset;
                    IsExpireTimestampVerified = true;
                }
                else
                {
                    spawnpoint = new Spawnpoint
                    {
                        Id = SpawnId ?? 0, 
                        Latitude = Latitude,
                        Longitude = Longitude,
                        Updated = DateTime.Now.ToTotalSeconds(),
                        DespawnSecond = null,
                    };
                }
                return await Task.FromResult(spawnpoint);
            }

            if (ExpireTimestamp == 0)
            {
                //Console.WriteLine($"[Pokemon] ExpireTimestamp == 0");
                ExpireTimestamp = DateTime.UtcNow.ToTotalSeconds();
                IsExpireTimestampVerified = false;
            }
            return null;
        }

        public ulong GetDespawnTimer(Spawnpoint spawnpoint)
        {
            var despawnSecond = spawnpoint.DespawnSecond;
            if (despawnSecond != null)
            {
                var now = DateTime.UtcNow.ToTotalSeconds();
                var unixDate = Updated.FromMilliseconds();
                var secondOfHour = unixDate.Second + unixDate.Minute * 60;
                var despawnOffset = despawnSecond ?? 0 - secondOfHour;
                if (despawnOffset < 0)
                {
                    despawnOffset += 3600;
                }
                return now + (ulong)despawnOffset;
            }
            return 0; // TODO: ?
        }

        public dynamic GetWebhookValues(string type)
        {
            return new
            {
                type  = "pokemon",
                message = new
                {
                    spawnpoint_id = SpawnId?.ToString() ?? "None", // TODO: ToHex 
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
                    capture_1 = CaptureRate1,
                    capture_2 = CaptureRate2,
                    capture_3 = CaptureRate3,
                    shiny = IsShiny,
                    username = Username,
                    display_pokemon_id = DisplayPokemonId,
                    pvp_rankings_great_league = PvpRankingsGreatLeague,
                    pvp_rankings_ultra_league = PvpRankingsUltraLeague,
                    is_event = IsEvent,
                },
            };
        }
    }
}