﻿namespace ChuckDeviceController.Data.Entities
{
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Text;

    using MySqlConnector;
    using POGOProtos.Rpc;

    using ChuckDeviceController.Common.Data;
    using ChuckDeviceController.Common.Data.Contracts;
    using ChuckDeviceController.Data.Contracts;
    using ChuckDeviceController.Data.Repositories;
    using ChuckDeviceController.Extensions;
    using ChuckDeviceController.Extensions.Http.Caching;
    using ChuckDeviceController.Geometry.Extensions;

    [Table("pokemon")]
    public class Pokemon : BaseEntity, IPokemon, ICoordinateEntity, IWebhookEntity, IEquatable<Pokemon>
    {
        #region Constants

        public const uint DefaultTimeUnseenS = 1200;
        public const uint DefaultTimeReseenS = 600;
        public const uint DittoPokemonId = (ushort)HoloPokemonId.Ditto; // 132
        public const ushort DittoTransformFastMove = (ushort)HoloPokemonMove.TransformFast; // 242
        public const ushort DittoStruggleChargeMove = (ushort)HoloPokemonMove.Struggle; // 133
        public const uint WeatherBoostMinLevel = 6;
        public const uint WeatherBoostMinIvStat = 4;
        public const bool PvpEnabled = true; // TODO: Make 'EnablePvp' configurable via config
        public const bool CellPokemonEnabled = true; // TODO: Make 'CellPokemonEnabled' configurable via config
        public const bool SaveSpawnpointLastSeen = true; // TODO: Make 'SaveSpawnpointLastSeen' configurable via config
        public const bool WeatherIvClearingEnabled = true; // TODO: Make 'WeatherIvClearingEnabled' configurable via config

        #endregion

        #region Properties

        [
            Column("id"),
            Key,
            DatabaseGenerated(DatabaseGeneratedOption.None),
        ]
        public string Id { get; set; } = null!;

        [Column("pokemon_id")]
        public uint PokemonId { get; set; }

        [Column("lat")]
        public double Latitude { get; set; }

        [Column("lon")]
        public double Longitude { get; set; }

        [
            Column("spawn_id"),
            ForeignKey("spawn_id"),
        ]
        public ulong? SpawnId { get; set; }

        //public virtual Spawnpoint? Spawnpoint { get; set; }

        [Column("expire_timestamp")]
        public ulong ExpireTimestamp { get; set; }

        [Column("atk_iv")]
        public ushort? AttackIV { get; set; }

        [Column("def_iv")]
        public ushort? DefenseIV { get; set; }

        [Column("sta_iv")]
        public ushort? StaminaIV { get; set; }

        [
            DatabaseGenerated(DatabaseGeneratedOption.Computed),
            Column("iv"),
        ]
        public double? IV { get; set; }

        [Column("move_1")]
        public ushort? Move1 { get; set; }

        [Column("move_2")]
        public ushort? Move2 { get; set; }

        [Column("gender")]
        public ushort? Gender { get; set; }

        [Column("form")]
        public ushort? Form { get; set; }

        [Column("costume")]
        public ushort? Costume { get; set; }

        [Column("cp")]
        public ushort? CP { get; set; }

        [Column("level")]
        public ushort? Level { get; set; }

        [Column("weight")]
        public double? Weight { get; set; }

        [Column("size")]
        public double? Size { get; set; }

        [Column("weather")]
        public ushort? Weather { get; set; }

        [Column("shiny")]
        public bool? IsShiny { get; set; }

        [Column("username")]
        public string? Username { get; set; }

        [
            Column("pokestop_id"),
            DefaultValue(null),
            ForeignKey("pokestop_id"),
        ]
        public string? PokestopId { get; set; } = null!;

        public virtual Pokestop? Pokestop { get; set; }

        [Column("first_seen_timestamp")]
        public ulong? FirstSeenTimestamp { get; set; }

        [Column("updated")]
        public ulong Updated { get; set; }

        [Column("changed")]
        public ulong Changed { get; set; }

        [
            Column("cell_id"),
            ForeignKey("cell_id"),
        ]
        public ulong CellId { get; set; }

        public virtual Cell? Cell { get; set; }

        [Column("expire_timestamp_verified")]
        public bool IsExpireTimestampVerified { get; set; }

        [Column("capture_1")]
        public double? Capture1 { get; set; }

        [Column("capture_2")]
        public double? Capture2 { get; set; }

        [Column("capture_3")]
        public double? Capture3 { get; set; }

        [Column("is_ditto")]
        public bool IsDitto { get; set; }

        [Column("display_pokemon_id")]
        public uint? DisplayPokemonId { get; set; }

        [Column("base_height")]
        public double BaseHeight { get; set; }

        [Column("base_weight")]
        public double BaseWeight { get; set; }

        [Column("is_event")]
        public bool IsEvent { get; set; }

        [Column("seen_type")]
        public SeenType SeenType { get; set; }

        [
            Column("pvp"),
            DefaultValue(null),
        ]
        public Dictionary<string, dynamic>? PvpRankings { get; set; } = null!;

        [NotMapped]
        public bool HasChanges { get; set; }

        [NotMapped]
        public bool HasIvChanges { get; set; }

        [NotMapped]
        public bool IsNewPokemon { get; set; }

        [NotMapped]
        public bool IsNewPokemonWithIV { get; set; }

        /// <summary>
        /// Gets a value determining whether the Pokemon was first seen more
        /// than 10 minutes ago and is close to expiring.
        /// </summary>
        [NotMapped]
        public bool IsExpirationSoon =>
            DateTime.UtcNow.ToTotalSeconds() - FirstSeenTimestamp >= DefaultTimeReseenS;

        [NotMapped]
        public bool SendWebhook { get; set; }

        #endregion

        #region Constructors

        public Pokemon()
        {
        }

        public static Pokemon ParsePokemonFromWild(WildPokemonProto wildPokemon, ulong cellId, string username, bool isEvent)
        {
            var spawnId = Convert.ToUInt64(wildPokemon.SpawnPointId, 16);
            var pokemon = new Pokemon
            {
                Id = wildPokemon.EncounterId.ToString(),
                PokemonId = Convert.ToUInt16(wildPokemon.Pokemon.PokemonId),
                Latitude = wildPokemon.Latitude,
                Longitude = wildPokemon.Longitude,
                SpawnId = spawnId,
                IsEvent = isEvent,
                Username = username,
                CellId = cellId,
                SeenType = SeenType.Wild,
            };
            pokemon.SetPokemonDisplay(wildPokemon.Pokemon?.PokemonDisplay);
            return pokemon;
        }

        public static async Task<Pokemon> ParsePokemonFromNearby(MySqlConnection connection, IMemoryCacheHostedService memCache, NearbyPokemonProto nearbyPokemon, ulong cellId, string username, bool isEvent)
        {
            var pokestopId = string.IsNullOrEmpty(nearbyPokemon.FortId)
                ? null
                : nearbyPokemon.FortId;
            var pokemon = new Pokemon
            {
                Id = Convert.ToString(nearbyPokemon.EncounterId),
                PokemonId = Convert.ToUInt16(nearbyPokemon.PokedexNumber),
                PokestopId = pokestopId,
                IsEvent = isEvent,
                Username = username,
                CellId = cellId,
                IsExpireTimestampVerified = false,
            };
            pokemon.SetPokemonDisplay(nearbyPokemon.PokemonDisplay);

            // Figure out where the Pokemon is
            double lat;
            double lon;
            if (string.IsNullOrEmpty(pokestopId))
            {
                if (!CellPokemonEnabled)
                {
                    return null;
                }
                // Set Pokemon location to S2 cell coordinate as an approximation
                var latlng = cellId.ToCoordinate();
                lat = latlng.Latitude;
                lon = latlng.Longitude;
                pokemon.SeenType = SeenType.NearbyCell;
            }
            else
            {
                var pokestop = await EntityRepository.GetEntityAsync<string, Pokestop>(connection, nearbyPokemon.FortId, memCache);
                if (pokestop == null)
                {
                    // Failed to fetch Pokestop for nearby Pokemon in order to find location, skipping
                    return null;
                }
                lat = pokestop.Latitude;
                lon = pokestop.Longitude;
                pokemon.SeenType = SeenType.NearbyStop;
            }

            pokemon.Latitude = lat;
            pokemon.Longitude = lon;

            return pokemon;
        }

        public static async Task<Pokemon> ParsePokemonFromMap(MySqlConnection connection, IMemoryCacheHostedService memCache, MapPokemonProto mapPokemon, ulong cellId, string username, bool isEvent)
        {
            var encounterId = Convert.ToUInt64(mapPokemon.EncounterId);
            var spawnpointId = mapPokemon.SpawnpointId;
            var pokemon = new Pokemon
            {
                Id = encounterId.ToString(),
                PokemonId = Convert.ToUInt32(mapPokemon.PokedexTypeId),
                IsEvent = isEvent,
                Username = username,
                CellId = cellId,
                SeenType = SeenType.LureWild,
            };
            pokemon.SetPokemonDisplay(mapPokemon.PokemonDisplay);
            
            // Get Pokestop via spawnpoint id
            var pokestop = await EntityRepository.GetEntityAsync<string, Pokestop>(connection, spawnpointId, memCache);
            if (pokestop == null)
            {
                Console.WriteLine($"Failed to fetch Pokestop by spawnpoint ID '{spawnpointId}' for map/lure Pokemon '{pokemon.Id}' to find location, skipping");
                return null;
            }
            pokemon.PokestopId = pokestop.Id;
            pokemon.Latitude = pokestop.Latitude;
            pokemon.Longitude = pokestop.Longitude;

            if (mapPokemon.ExpirationTimeMs > 0)
            {
                pokemon.ExpireTimestamp = Convert.ToUInt64((0 + Convert.ToUInt64(mapPokemon.ExpirationTimeMs)) / 1000);
                pokemon.IsExpireTimestampVerified = true;
            }
            else
            {
                pokemon.IsExpireTimestampVerified = false;
            }

            return pokemon;
        }

        //public Pokemon(WildPokemonProto wildPokemon, ulong cellId, string username, bool isEvent)
        //{
        //    IsEvent = isEvent;
        //    Id = wildPokemon.EncounterId.ToString();
        //    PokemonId = Convert.ToUInt16(wildPokemon.Pokemon.PokemonId);
        //    Latitude = wildPokemon.Latitude;
        //    Longitude = wildPokemon.Longitude;
        //    var spawnId = Convert.ToUInt64(wildPokemon.SpawnPointId, 16);
        //    Gender = Convert.ToUInt16(wildPokemon.Pokemon.PokemonDisplay.Gender);
        //    Form = Convert.ToUInt16(wildPokemon.Pokemon.PokemonDisplay.Form);
        //    if (wildPokemon.Pokemon.PokemonDisplay != null)
        //    {
        //        Costume = Convert.ToUInt16(wildPokemon.Pokemon.PokemonDisplay.Costume);
        //        Weather = Convert.ToUInt16(wildPokemon.Pokemon.PokemonDisplay.WeatherBoostedCondition);
        //    }
        //    Username = username;
        //    SpawnId = spawnId;
        //    CellId = cellId;
        //    SeenType = SeenType.Wild;
        //}

        //public Pokemon(MySqlConnection connection, IMemoryCacheHostedService memCache, NearbyPokemonProto nearbyPokemon, ulong cellId, string username, bool isEvent)
        //{
        //    Id = Convert.ToString(nearbyPokemon.EncounterId);

        //    // Figure out where the Pokemon is
        //    double lat;
        //    double lon;
        //    if (string.IsNullOrEmpty(PokestopId))
        //    {
        //        if (!CellPokemonEnabled)
        //        {
        //            return;
        //        }
        //        // Set Pokemon location to S2 cell coordinate as an approximation
        //        var latlng = cellId.ToCoordinate();
        //        lat = latlng.Latitude;
        //        lon = latlng.Longitude;
        //        SeenType = SeenType.NearbyCell;
        //    }
        //    else
        //    {
        //        var pokestop = EntityRepository.GetEntityAsync<string, Pokestop>(connection, nearbyPokemon.FortId, memCache).Result;
        //        if (pokestop == null)
        //        {
        //            Console.WriteLine($"Failed to fetch Pokestop for nearby Pokemon '{Id}' to find location, skipping");
        //            return;
        //        }
        //        lat = pokestop.Latitude;
        //        lon = pokestop.Longitude;
        //        SeenType = SeenType.NearbyStop;
        //    }

        //    Latitude = lat;
        //    Longitude = lon;
        //    PokemonId = Convert.ToUInt16(nearbyPokemon.PokedexNumber);
        //    PokestopId = string.IsNullOrEmpty(nearbyPokemon.FortId)
        //        ? null
        //        : nearbyPokemon.FortId;
        //    if (nearbyPokemon.PokemonDisplay != null)
        //    {
        //        Form = Convert.ToUInt16(nearbyPokemon.PokemonDisplay.Form);
        //        Costume = Convert.ToUInt16(nearbyPokemon.PokemonDisplay.Costume);
        //        Weather = Convert.ToUInt16(nearbyPokemon.PokemonDisplay.WeatherBoostedCondition);
        //        Gender = Convert.ToUInt16(nearbyPokemon.PokemonDisplay.Gender);
        //    }
        //    IsEvent = isEvent;
        //    Username = username;
        //    CellId = cellId;
        //    IsExpireTimestampVerified = false;
        //}

        //public Pokemon(MySqlConnection connection, IMemoryCacheHostedService memCache, MapPokemonProto mapPokemon, ulong cellId, string username, bool isEvent)
        //{
        //    var encounterId = Convert.ToUInt64(mapPokemon.EncounterId);
        //    Id = encounterId.ToString();
        //    PokemonId = Convert.ToUInt32(mapPokemon.PokedexTypeId);

        //    var spawnpointId = mapPokemon.SpawnpointId;
        //    // Get Pokestop via spawnpoint id
        //    var pokestop = EntityRepository.GetEntityAsync<string, Pokestop>(connection, spawnpointId, memCache).Result;
        //    if (pokestop == null)
        //    {
        //        Console.WriteLine($"Failed to fetch Pokestop by spawnpoint ID '{spawnpointId}' for map/lure Pokemon '{Id}' to find location, skipping");
        //        return;
        //    }
        //    PokestopId = pokestop.Id;
        //    Latitude = pokestop.Latitude;
        //    Longitude = pokestop.Longitude;

        //    if (mapPokemon.PokemonDisplay != null)
        //    {
        //        Gender = Convert.ToUInt16(mapPokemon.PokemonDisplay.Gender);
        //        Form = Convert.ToUInt16(mapPokemon.PokemonDisplay.Form);
        //        Costume = Convert.ToUInt16(mapPokemon.PokemonDisplay.Costume);
        //        Weather = Convert.ToUInt16(mapPokemon.PokemonDisplay.WeatherBoostedCondition);
        //    }

        //    Username = username;
        //    if (mapPokemon.ExpirationTimeMs > 0)
        //    {
        //        ExpireTimestamp = Convert.ToUInt64((0 + Convert.ToUInt64(mapPokemon.ExpirationTimeMs)) / 1000);
        //        IsExpireTimestampVerified = true;
        //    }
        //    else
        //    {
        //        IsExpireTimestampVerified = false;
        //    }

        //    IsEvent = isEvent;
        //    SeenType = SeenType.LureWild;
        //    CellId = cellId;
        //}

        #endregion

        #region Public Methods

        public void AddEncounter(EncounterOutProto encounterData, string username)
        {
            var pokemonId = Convert.ToUInt32(encounterData.Pokemon.Pokemon.PokemonId);
            var cp = Convert.ToUInt16(encounterData.Pokemon.Pokemon.Cp);
            var move1 = Convert.ToUInt16(encounterData.Pokemon.Pokemon.Move1);
            var move2 = Convert.ToUInt16(encounterData.Pokemon.Pokemon.Move2);
            var size = Convert.ToDouble(encounterData.Pokemon.Pokemon.HeightM);
            var weight = Convert.ToDouble(encounterData.Pokemon.Pokemon.WeightKg);
            var atkIv = Convert.ToUInt16(encounterData.Pokemon.Pokemon.IndividualAttack);
            var defIv = Convert.ToUInt16(encounterData.Pokemon.Pokemon.IndividualDefense);
            var staIv = Convert.ToUInt16(encounterData.Pokemon.Pokemon.IndividualStamina);
            var costume = Convert.ToUInt16(encounterData.Pokemon.Pokemon.PokemonDisplay.Costume);
            var form = Convert.ToUInt16(encounterData.Pokemon.Pokemon.PokemonDisplay.Form);
            var gender = Convert.ToUInt16(encounterData.Pokemon.Pokemon.PokemonDisplay.Gender);
            var weather = Convert.ToUInt16(encounterData.Pokemon.Pokemon.PokemonDisplay.WeatherBoostedCondition);
            var lat = Convert.ToDouble(encounterData.Pokemon.Latitude);
            var lon = Convert.ToDouble(encounterData.Pokemon.Longitude);

            if (pokemonId != PokemonId ||
                cp != CP ||
                move1 != Move1 ||
                move2 != Move2 ||
                size != Size ||
                weight != Weight ||
                AttackIV != atkIv ||
                DefenseIV != defIv ||
                StaminaIV != staIv ||
                Costume != Costume ||
                Form != form ||
                Gender != gender ||
                Weather != weather)
            {
                HasChanges = true;
                HasIvChanges = true;
            }

            PokemonId = pokemonId;
            CP = cp;
            Move1 = move1;
            Move2 = move2;
            Size = size;
            Weight = weight;
            AttackIV = atkIv;
            DefenseIV = defIv;
            StaminaIV = staIv;
            Costume = costume;
            Form = form;
            Gender = gender;
            Weather = weather;
            Latitude = lat;
            Longitude = lon;

            IsShiny = encounterData.Pokemon.Pokemon.PokemonDisplay.Shiny;
            Username = username;

            if (HasIvChanges)
            {
                // Although capture change values are player specific, set them to the Pokemon
                // Should remove them eventually though.
                if (encounterData.CaptureProbability != null)
                {
                    Capture1 = encounterData.CaptureProbability.CaptureProbability[0];
                    Capture2 = encounterData.CaptureProbability.CaptureProbability[1];
                    Capture3 = encounterData.CaptureProbability.CaptureProbability[2];
                }

                // Calculate Pokemon level from provided CP multiplier value
                var cpMultiplier = encounterData.Pokemon.Pokemon.CpMultiplier;
                Level = CalculateLevel(cpMultiplier);

                IsDitto = IsDittoDisguised(this);
                if (IsDitto)
                {
                    // Set default Ditto attributes
                    SetDittoAttributes(PokemonId, Weather ?? 0, Level ?? 0);
                }
            }

            var wildPokemon = encounterData.Pokemon;
            var spawnId = Convert.ToUInt64(wildPokemon.SpawnPointId, 16);
            SpawnId = spawnId;
            SeenType = SeenType.Encounter;
            Updated = DateTime.UtcNow.ToTotalSeconds();
            Changed = Updated;
        }

        public void AddDiskEncounter(DiskEncounterOutProto diskEncounterData, string username)
        {
            var pokemonId = Convert.ToUInt32(diskEncounterData.Pokemon.PokemonId);
            var cp = Convert.ToUInt16(diskEncounterData.Pokemon.Cp);
            var move1 = Convert.ToUInt16(diskEncounterData.Pokemon.Move1);
            var move2 = Convert.ToUInt16(diskEncounterData.Pokemon.Move2);
            var size = Convert.ToDouble(diskEncounterData.Pokemon.HeightM);
            var weight = Convert.ToDouble(diskEncounterData.Pokemon.WeightKg);
            var atkIv = Convert.ToUInt16(diskEncounterData.Pokemon.IndividualAttack);
            var defIv = Convert.ToUInt16(diskEncounterData.Pokemon.IndividualDefense);
            var staIv = Convert.ToUInt16(diskEncounterData.Pokemon.IndividualStamina);
            var costume = Convert.ToUInt16(diskEncounterData.Pokemon.PokemonDisplay.Costume);
            var form = Convert.ToUInt16(diskEncounterData.Pokemon.PokemonDisplay.Form);
            var gender = Convert.ToUInt16(diskEncounterData.Pokemon.PokemonDisplay.Gender);

            if (pokemonId != PokemonId ||
                cp != CP ||
                move1 != Move1 ||
                move2 != Move2 ||
                size != Size ||
                weight != Weight ||
                AttackIV != atkIv ||
                DefenseIV != defIv ||
                StaminaIV != staIv ||
                Costume != Costume ||
                Form != form ||
                Gender != gender)
            {
                HasChanges = true;
                HasIvChanges = true;
            }

            PokemonId = pokemonId;
            CP = cp;
            Move1 = move1;
            Move2 = move2;
            Size = size;
            Weight = weight;
            AttackIV = atkIv;
            DefenseIV = defIv;
            StaminaIV = staIv;
            Costume = costume;
            Form = form;
            Gender = gender;
            //Weather = weather;

            IsShiny = diskEncounterData.Pokemon?.PokemonDisplay?.Shiny ?? false;
            Username = username;

            if (HasIvChanges)
            {
                // Although capture change values are player specific, set them to the Pokemon
                // Should remove them eventually though.
                if (diskEncounterData.CaptureProbability != null)
                {
                    Capture1 = diskEncounterData.CaptureProbability.CaptureProbability[0];
                    Capture2 = diskEncounterData.CaptureProbability.CaptureProbability[1];
                    Capture3 = diskEncounterData.CaptureProbability.CaptureProbability[2];
                }
                var cpMultiplier = diskEncounterData.Pokemon?.CpMultiplier ?? 0;
                Level = CalculateLevel(cpMultiplier);

                IsDitto = IsDittoDisguised(this);
                if (IsDitto)
                {
                    SetDittoAttributes(PokemonId, Weather ?? 0, Level ?? 0);
                }
            }

            SeenType = SeenType.LureEncounter;
            Updated = DateTime.UtcNow.ToTotalSeconds();
            Changed = Updated;
        }

        //public async Task UpdateAsync(MySqlConnection connection, IMemoryCacheHostedService memCache, bool updateIv = false, bool skipLookup = false)
        public async Task UpdateAsync(Pokemon? oldPokemon, IMemoryCacheHostedService memCache, bool updateIv = false)
        {
            var updateIV = updateIv;
            var setIvForWeather = false;
            var now = DateTime.UtcNow.ToTotalSeconds();
            Updated = now;

            if (IsEvent && AttackIV == null)
            {
                // TODO: Pokemon? oldPokemonNoEvent = null;
                var oldPokemonNoEvent = await EntityRepository.GetEntityAsync<string, Pokemon>(null, Id, memCache); // IsEvent: false;
                if (oldPokemonNoEvent != null && oldPokemonNoEvent.AttackIV != null &&
                    (((Weather == 0 || Weather == null) && (oldPokemonNoEvent.Weather == 0 || oldPokemonNoEvent.Weather == null)) ||
                    (Weather != 0 && oldPokemonNoEvent.Weather != 0)))
                {
                    AttackIV = oldPokemonNoEvent.AttackIV;
                    DefenseIV = oldPokemonNoEvent.DefenseIV;
                    StaminaIV = oldPokemonNoEvent.StaminaIV;
                    Level = oldPokemonNoEvent.Level;
                    CP = null;
                    Weight = null;
                    Size = null;
                    Move1 = null;
                    Move2 = null;
                    Capture1 = null;
                    Capture2 = null;
                    Capture3 = null;
                    updateIV = true;
                    // TODO: SetPvpRankings();
                }
            }
            if (IsEvent && !IsExpireTimestampVerified)
            {
                // TODO: Pokemon? oldPokemonNoEvent = null;
                var oldPokemonNoEvent = await EntityRepository.GetEntityAsync<string, Pokemon>(null, Id, memCache); // IsEvent: false;
                if (oldPokemonNoEvent != null && oldPokemonNoEvent.IsExpireTimestampVerified)
                {
                    ExpireTimestamp = oldPokemonNoEvent.ExpireTimestamp;
                    IsExpireTimestampVerified = oldPokemonNoEvent.IsExpireTimestampVerified;
                }
            }

            if (oldPokemon == null)
            {
                setIvForWeather = false;

                ExpireTimestamp = ExpireTimestamp == 0
                    ? now + DefaultTimeUnseenS
                    : ExpireTimestamp;
                FirstSeenTimestamp = now;
                Updated = now;
                Changed = now;
            }
            else
            {
                if (FirstSeenTimestamp == null && oldPokemon.FirstSeenTimestamp > 0)
                {
                    FirstSeenTimestamp = oldPokemon.FirstSeenTimestamp;
                }

                if (ExpireTimestamp == 0)// && oldPokemon.ExpireTimestamp > 0)
                {
                    var changed = DateTime.UtcNow.ToTotalSeconds();
                    var oldExpireDate = oldPokemon.ExpireTimestamp;
                    if (changed - oldExpireDate < DefaultTimeReseenS || /* TODO: Workaround -> */ oldPokemon.ExpireTimestamp == 0)
                    {
                        ExpireTimestamp = changed + DefaultTimeReseenS;
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
                        Console.WriteLine($"Pokemon {Id} changed from {oldPokemon.PokemonId} to {PokemonId}");
                    }
                    else if (oldPokemon.DisplayPokemonId != PokemonId)
                    {
                        Console.WriteLine($"Pokemon {Id} Ditto disguised as {oldPokemon.DisplayPokemonId} now seen as {PokemonId}");
                    }
                    else if (oldPokemon.DisplayPokemonId != null && oldPokemon.PokemonId != PokemonId)
                    {
                        Console.WriteLine($"Pokemon {Id} Ditto from {oldPokemon.PokemonId} to {PokemonId}");
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

                if (oldPokemon.PokestopId != null && PokestopId == null)
                {
                    PokestopId = oldPokemon.PokestopId;
                }

                if (oldPokemon.PvpRankings != null && PvpRankings == null)
                {
                    PvpRankings = oldPokemon.PvpRankings;
                }

                if (updateIV && oldPokemon.AttackIV == null && AttackIV != null)
                {
                    Changed = now;
                }
                else
                {
                    Changed = oldPokemon.Changed;
                }

                var weatherChanged = ((oldPokemon.Weather == null || oldPokemon.Weather == 0) && (Weather > 0)) ||
                    ((Weather == null || Weather == 0) && (oldPokemon.Weather > 0));

                if (oldPokemon.AttackIV != null && AttackIV == null && !weatherChanged)
                {
                    setIvForWeather = false;
                    AttackIV = oldPokemon.AttackIV;
                    DefenseIV = oldPokemon.DefenseIV;
                    StaminaIV = oldPokemon.StaminaIV;
                    CP = oldPokemon.CP;
                    Weight = oldPokemon.Weight;
                    Size = oldPokemon.Size;
                    Move1 = oldPokemon.Move1;
                    Move2 = oldPokemon.Move2;
                    Level = oldPokemon.Level;
                    Capture1 = oldPokemon.Capture1;
                    Capture2 = oldPokemon.Capture2;
                    Capture3 = oldPokemon.Capture3;
                    IsShiny = oldPokemon.IsShiny;
                    SeenType = oldPokemon.SeenType;
                    IsDitto = IsDittoDisguised(oldPokemon);

                    if (IsDitto)
                    {
                        Console.WriteLine($"oldPokemon {Id} Ditto found, disguised as {PokemonId}");
                        SetDittoAttributes(PokemonId, oldPokemon.Weather ?? 0, oldPokemon.Level ?? 0);
                    }
                }
                else if ((AttackIV != null && oldPokemon.AttackIV == null) ||
                    (CP != null && oldPokemon.CP == null) || HasIvChanges)
                {
                    setIvForWeather = false;
                    updateIV = true;
                }
                else if (weatherChanged && oldPokemon.AttackIV != null && WeatherIvClearingEnabled)
                {
                    Console.WriteLine($"Pokemon {Id} changed weather boost state. Clearing IVs.");
                    setIvForWeather = true;
                    AttackIV = null;
                    DefenseIV = null;
                    StaminaIV = null;
                    CP = null;
                    Weight = null;
                    Size = null;
                    Move1 = null;
                    Move2 = null;
                    Level = null;
                    Capture1 = null;
                    Capture2 = null;
                    Capture3 = null;
                    PvpRankings = null;

                    Console.WriteLine($"Weather-Boosted state changed. Clearing IVs");
                }
                else
                {
                    setIvForWeather = false;
                }

                if (updateIV || setIvForWeather)
                {
                    HasIvChanges = true;
                }

                if (oldPokemon.PokemonId == DittoPokemonId && PokemonId != DittoPokemonId)
                {
                    Console.WriteLine($"Pokemon {Id} Ditto changed from {oldPokemon.PokemonId} to {PokemonId}");
                }

                Updated = now;
            }

            if (setIvForWeather)
            {
                SendWebhook = true;
                IsNewPokemon = true;
            }
            else if (oldPokemon == null)
            {
                SendWebhook = true;
                IsNewPokemon = true;
                IsNewPokemonWithIV = AttackIV != null;
            }
            else if (updateIV && ((oldPokemon.AttackIV == null && AttackIV != null) || oldPokemon.HasIvChanges))
            {
                SendWebhook = true;
                oldPokemon.HasIvChanges = false;
                IsNewPokemonWithIV = true;
            }

            // Cache pokemon entity by id
            // TODO: memCache.Set(Id, this);

            await Task.CompletedTask;
        }

        public async Task<Spawnpoint?> ParseSpawnpointAsync(MySqlConnection connection, IMemoryCacheHostedService memCache, int timeTillHiddenMs, ulong timestampMs)
        {
            var spawnId = SpawnId ?? 0;
            if (spawnId == 0)
            {
                return null;
            }

            var now = DateTime.UtcNow.ToTotalSeconds();
            if (timeTillHiddenMs <= 90000 && timeTillHiddenMs > 0)
            {
                ExpireTimestamp = Convert.ToUInt64((timestampMs + Convert.ToUInt64(timeTillHiddenMs)) / 1000);
                IsExpireTimestampVerified = true;
                var date = timestampMs.FromMilliseconds();
                var secondOfHour = date.Second + (date.Minute * 60);

                var spawnpoint = new Spawnpoint
                {
                    Id = spawnId,
                    Latitude = Latitude,
                    Longitude = Longitude,
                    DespawnSecond = Convert.ToUInt16(secondOfHour),
                    LastSeen = SaveSpawnpointLastSeen ? now : null,
                    Updated = now,
                };
                await spawnpoint.UpdateAsync(connection, memCache, update: true, skipLookup: true);
                return spawnpoint;
            }

            IsExpireTimestampVerified = false;

            if (!IsExpireTimestampVerified && spawnId > 0)
            {
                var oldSpawnpoint = await EntityRepository.GetEntityAsync<ulong, Spawnpoint>(connection, SpawnId ?? 0, memCache);
                if (oldSpawnpoint != null && oldSpawnpoint.DespawnSecond != null)
                {
                    var despawnSecond = oldSpawnpoint.DespawnSecond;
                    var timestampS = timestampMs / 1000;
                    var date = timestampS.FromMilliseconds();
                    var secondOfHour = date.Second + (date.Minute * 60);
                    var despawnOffset = despawnSecond - secondOfHour;
                    if (despawnSecond < secondOfHour)
                        despawnOffset += 3600;

                    // Update spawnpoint last_seen if enabled
                    if (SaveSpawnpointLastSeen)
                    {
                        oldSpawnpoint.LastSeen = now;
                    }

                    ExpireTimestamp = timestampS + (ulong)despawnOffset;
                    IsExpireTimestampVerified = true;
                    return oldSpawnpoint;
                }
                else
                {
                    var newSpawnpoint = new Spawnpoint
                    {
                        Id = spawnId,
                        Latitude = Latitude,
                        Longitude = Longitude,
                        DespawnSecond = null,
                        LastSeen = SaveSpawnpointLastSeen ? now : null,
                        Updated = now,
                    };
                    await newSpawnpoint.UpdateAsync(connection, memCache, update: true, skipLookup: true);
                    return newSpawnpoint;
                }
            }

            return null;
        }

        public dynamic? GetWebhookData(string type)
        {
            switch (type.ToLower())
            {
                case "pokemon":
                    return new
                    {
                        type = "pokemon",
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
                            capture_1 = Capture1,
                            capture_2 = Capture2,
                            capture_3 = Capture3,
                            shiny = IsShiny,
                            username = Username,
                            display_pokemon_id = DisplayPokemonId,
                            pvp = PvpRankings,
                            is_event = IsEvent,
                            seen_type = SeenType,
                        },
                    };
            }

            Console.WriteLine($"Received unknown pokemon webhook payload type: {type}, returning null");
            return null;
        }

        #endregion

        #region Private Methods

        private static ushort CalculateLevel(double cpMultiplier)
        {
            ushort level;
            if (cpMultiplier < 0.734)
            {
                level = Convert.ToUInt16(Math.Round(58.35178527 * cpMultiplier * cpMultiplier - 2.838007664 * cpMultiplier + 0.8539209906));
            }
            else
            {
                level = Convert.ToUInt16(Math.Round(171.0112688 * cpMultiplier - 95.20425243));
            }
            return level;
        }

        private void SetPokemonDisplay(PokemonDisplayProto? pokemonDisplay)
        {
            var hasDisplay = pokemonDisplay != null;
            Form = hasDisplay
                ? Convert.ToUInt16(pokemonDisplay?.Form ?? PokemonDisplayProto.Types.Form.Unset)
                : (ushort)0;
            Costume = hasDisplay
                ? Convert.ToUInt16(pokemonDisplay?.Costume ?? PokemonDisplayProto.Types.Costume.Unset)
                : (ushort)0;
            Gender = hasDisplay
                ? Convert.ToUInt16(pokemonDisplay?.Gender ?? PokemonDisplayProto.Types.Gender.Unset)
                : (ushort)0;
            Weather = hasDisplay
                ? Convert.ToUInt16(pokemonDisplay?.WeatherBoostedCondition ?? GameplayWeatherProto.Types.WeatherCondition.None)
                : (ushort)0;
        }

        #endregion

        #region Ditto Detection

        private void SetDittoAttributes(uint displayPokemonId, ushort weather, ushort level)
        {
            DisplayPokemonId = displayPokemonId;
            PokemonId = DittoPokemonId;
            Form = 0;
            Move1 = DittoTransformFastMove;
            Move2 = DittoStruggleChargeMove;
            Gender = 3;
            Costume = 0;
            Size = null;
            Weight = null;
            if (weather == 0 && level > 30)
            {
                Console.WriteLine($"Pokemon {Id} weather boosted Ditto - reset IV is needed");
            }
        }

        public static bool IsDittoDisguised(Pokemon pokemon)
        {
            if (pokemon.PokemonId == DittoPokemonId)
            {
                Console.WriteLine($"Pokemon {pokemon.Id} was already detected as Ditto.");
                return true;
            }

            var level = pokemon.Level;
            var isUnderLevelBoosted = level > 0 && level < WeatherBoostMinLevel;
            var isUnderIvStatBoosted = level > 0 &&
                (pokemon.AttackIV < WeatherBoostMinIvStat ||
                 pokemon.DefenseIV < WeatherBoostMinIvStat ||
                 pokemon.StaminaIV < WeatherBoostMinIvStat);
            var isWeatherBoosted = pokemon.Weather > 0;
            var isOverLevel = level > 30;

            if (isWeatherBoosted)
            {
                if (isUnderLevelBoosted || isUnderIvStatBoosted)
                {
                    Console.WriteLine($"Pokemon {pokemon.Id} Ditto found, disguised as {pokemon.PokemonId}");
                    return true;
                }
            }
            else
            {
                if (isOverLevel)
                {
                    Console.WriteLine($"Pokemon {pokemon.Id} weather boosted Ditto found, disguised as {pokemon.PokemonId}");
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region Helper Methods

        public static string SeenTypeToString(SeenType type) => type.ToString();

        public static SeenType StringToSeenType(string seenType) => (SeenType)seenType;

        #endregion

        #region Comparison Methods

        public bool Equals(Pokemon? other)
        {
            if (other == null)
                return false;

            var result = 
                Id == other.Id &&
                PokemonId == other.PokemonId &&
                Form == other.Form &&
                Costume == other.Costume &&
                Gender == other.Gender &&
                Level == other.Level &&
                CP == other.CP &&
                AttackIV == other.AttackIV &&
                DefenseIV == other.DefenseIV &&
                StaminaIV == other.StaminaIV &&
                Move1 == other.Move1 &&
                Move2 == other.Move2 &&
                DisplayPokemonId == other.DisplayPokemonId &&
                SpawnId == other.SpawnId &&
                Size == other.Size &&
                Weight == other.Weight &&
                CellId == other.CellId &&
                Weather == other.Weather &&
                Username == other.Username &&
                PokestopId == other.PokestopId &&
                Latitude == other.Latitude &&
                Longitude == other.Longitude;
            return result;
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as Pokemon);
        }

        public override int GetHashCode()
        {
            return (int)PokemonId ^
                (Form ?? -1) ^
                (Costume ?? -1) ^
                (Gender ?? -1) ^
                (AttackIV ?? -1) ^
                (DefenseIV ?? -1) ^
                (StaminaIV ?? -1);
        }

        #endregion

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(PokemonId);
            if ((Form ?? 0) > 0)    sb.Append($"_f{Form}");
            if ((Costume ?? 0) > 0) sb.Append($"_c{Costume}");
            if ((Gender ?? 0) > 0)  sb.Append($"_g{Gender}");
            var id = sb.ToString();
            return id;
        }
    }

    /*
    public class DittoDetector
    {
        private const uint DittoPokemonId = (ushort)HoloPokemonId.Ditto; // 132
        private const ushort DittoTransformFastMove = (ushort)HoloPokemonMove.TransformFast; // 242
        private const ushort DittoStruggleChargeMove = (ushort)HoloPokemonMove.Struggle; // 133
        private const uint WeatherBoostMinLevel = 6;
        private const uint WeatherBoostMinIvStat = 4;

        private readonly Pokemon _pokemon;

        public DittoDetector(Pokemon pokemon)
        {
            _pokemon = pokemon;
        }

        public bool IsDittoDisguised()
        {
            if (_pokemon.PokemonId == DittoPokemonId)
            {
                Console.WriteLine($"Pokemon {_pokemon.Id} was already detected as Ditto.");
                return true;
            }

            var level = _pokemon.Level;
            var isUnderLevelBoosted = level > 0 && level < WeatherBoostMinLevel;
            var isUnderIvStatBoosted = level > 0 &&
                (_pokemon.AttackIV < WeatherBoostMinIvStat ||
                 _pokemon.DefenseIV < WeatherBoostMinIvStat ||
                 _pokemon.StaminaIV < WeatherBoostMinIvStat);
            var isWeatherBoosted = _pokemon.Weather > 0;
            var isOverLevel = level > 30;

            if (isWeatherBoosted)
            {
                if (isUnderLevelBoosted || isUnderIvStatBoosted)
                {
                    Console.WriteLine($"Pokemon {_pokemon.Id} Ditto found, disguised as {_pokemon.PokemonId}");
                    return true;
                }
            }
            else
            {
                if (isOverLevel)
                {
                    Console.WriteLine($"Pokemon {_pokemon.Id} weather boosted Ditto found, disguised as {_pokemon.PokemonId}");
                    return true;
                }
            }

            return false;
        }
    }
    */
}