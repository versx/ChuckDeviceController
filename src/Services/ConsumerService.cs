namespace ChuckDeviceController.Services
{
    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Data.Repositories;
    using ChuckDeviceController.Extensions;
    using ChuckDeviceController.JobControllers;
    using ChuckDeviceController.Net.Webhooks;
    using ChuckDeviceController.Services.Models;
    using Google.Common.Geometry;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using POGOProtos.Rpc;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;

    // TODO: Use Redis rpush/blpop event to send data (maybe create seperate service for db parsing)

    public interface IConsumerService
    {
        Task AddData(ConsumerData data);
    }

    public class ConsumerService : IConsumerService
    {
        public int MaxConcurrency = 100;

        #region Variables

        // Dependency injection variables
        //private readonly IDbContextFactory<DeviceControllerContext> _contextFactory;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ConsumerService> _logger;

        // Global lists
        private readonly List<dynamic> _wildPokemon;
        private readonly List<dynamic> _nearbyPokemon;
        private readonly List<ClientWeatherProto> _clientWeather;
        private readonly List<dynamic> _forts;
        private readonly List<FortDetailsOutProto> _fortDetails;
        private readonly List<GymGetInfoOutProto> _gymInfos;
        private readonly List<QuestProto> _quests;
        private readonly List<FortSearchOutProto> _fortSearch;
        private readonly List<dynamic> _encounters;
        //private readonly List<Spawnpoint> _spawnpoints;
        private readonly List<ulong> _cells;
        private readonly List<InventoryDeltaProto> _inventory;
        private readonly List<dynamic> _playerData;

        private readonly Dictionary<ulong, List<string>> _gymIdsPerCell;
        private readonly Dictionary<ulong, List<string>> _stopIdsPerCell;

        // Locks
        private readonly object _nearbyPokemonLock = new object();
        private readonly object _wildPokemonLock = new object();
        private readonly object _fortsLock = new object();
        private readonly object _fortDetailsLock = new object();
        private readonly object _gymInfosLock = new object();
        private readonly object _questsLock = new object();
        private readonly object _fortSearchLock = new object();
        private readonly object _encountersLock = new object();
        private readonly object _weatherLock = new object();
        //private readonly object _spawnpointsLock = new object();
        private readonly object _cellsLock = new object();
        private readonly object _inventoryLock = new object();
        private readonly object _playerDataLock = new object();

        private readonly System.Timers.Timer _timer;

        #endregion

        #region Singleton

        /*
        private static IConsumerService _instance;
        public static IConsumerService Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ConsumerService();
                }
                return _instance;
            }
        }
        */

        #endregion

        #region Constructor

        public ConsumerService()
        {
            _wildPokemon = new List<dynamic>();
            _nearbyPokemon = new List<dynamic>();
            _clientWeather = new List<ClientWeatherProto>();
            _forts = new List<dynamic>();
            _fortDetails = new List<FortDetailsOutProto>();
            _fortSearch = new List<FortSearchOutProto>();
            _gymInfos = new List<GymGetInfoOutProto>();
            _quests = new List<QuestProto>();
            _encounters = new List<dynamic>();
            _cells = new List<ulong>();
            //_spawnpoints = new List<Spawnpoint>();
            _inventory = new List<InventoryDeltaProto>();
            _playerData = new List<dynamic>();

            _gymIdsPerCell = new Dictionary<ulong, List<string>>();
            _stopIdsPerCell = new Dictionary<ulong, List<string>>();
        }

        public ConsumerService(IServiceScopeFactory scopeFactory, ILogger<ConsumerService> logger) : this()
        {
            _scopeFactory = scopeFactory;
            _logger = logger;

            _timer = new System.Timers.Timer
            {
                Interval = 500,
            };
            _timer.Elapsed += OnTimerElapsed;
            _timer.Start();
        }

        #endregion

        #region Public Methods

        public async Task AddData(ConsumerData data)
        {
            try
            {
                if (data.WildPokemon.Count > 0)
                {
                    lock (_wildPokemonLock)
                    {
                        _wildPokemon.AddRange(data.WildPokemon);
                    }
                }
                if (data.NearbyPokemon.Count > 0)
                {
                    lock (_nearbyPokemonLock)
                    {
                        _nearbyPokemon.AddRange(data.NearbyPokemon);
                    }
                }
                if (data.ClientWeather.Count > 0)
                {
                    lock (_weatherLock)
                    {
                        _clientWeather.AddRange(data.ClientWeather);
                    }
                }
                if (data.Forts.Count > 0)
                {
                    lock (_fortsLock)
                    {
                        _forts.AddRange(data.Forts);
                    }
                }
                if (data.FortDetails.Count > 0)
                {
                    lock (_fortDetailsLock)
                    {
                        _fortDetails.AddRange(data.FortDetails);
                    }
                }
                if (data.GymInfo.Count > 0)
                {
                    lock (_gymInfosLock)
                    {
                        _gymInfos.AddRange(data.GymInfo);
                    }
                }
                if (data.Quests.Count > 0)
                {
                    lock (_questsLock)
                    {
                        _quests.AddRange(data.Quests);
                    }
                }
                if (data.FortSearch.Count > 0)
                {
                    lock (_fortSearchLock)
                    {
                        _fortSearch.AddRange(data.FortSearch);
                    }
                }
                if (data.Encounters.Count > 0)
                {
                    lock (_encountersLock)
                    {
                        _encounters.AddRange(data.Encounters);
                    }
                }
                if (data.Cells.Count > 0)
                {
                    lock (_cellsLock)
                    {
                        _cells.AddRange(data.Cells);
                    }
                }
                /*
                if (data.Spawnpoints.Count > 0)
                {
                    lock (_spawnpointsLock)
                    {
                        _spawnpoints.AddRange(data.Spawnpoints);
                    }
                }
                */
                if (data.Inventory.Count > 0)
                {
                    lock (_inventoryLock)
                    {
                        _inventory.AddRange(data.Inventory);
                    }
                }
                if (data.PlayerData.Count > 0)
                {
                    lock (_playerDataLock)
                    {
                        _playerData.AddRange(data.PlayerData);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error AddData: {ex}");
            }
            await Task.CompletedTask.ConfigureAwait(false);
        }

        #endregion

        #region Private Methods

        private async void OnTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            // Loop through all entities and add/update in database
            // TODO: Possibly split each time into their own thread or timer instead of one for all
            // TODO: Check performance using just one context instead of one per entity repo
            if (_cells.Count > 0)
            {
                try
                {
                    await UpdateCells().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error: {ex}");
                }
            }
            if (_clientWeather.Count > 0)
            {
                await UpdateWeather().ConfigureAwait(false);
            }
            /*
            if (_spawnpoints.Count > 0)
            {
                await UpdateSpawnpoints();
            }
            */
            if (_forts.Count > 0)
            {
                await UpdateForts().ConfigureAwait(false);
            }
            if (_fortDetails.Count > 0)
            {
                await UpdateFortDetails().ConfigureAwait(false);
            }
            if (_fortSearch.Count > 0)
            {
                // TODO: FortSearch
            }
            if (_gymInfos.Count > 0)
            {
                await UpdateGymGetInfo().ConfigureAwait(false);
            }
            if (_quests.Count > 0)
            {
                await UpdateQuests().ConfigureAwait(false);
            }
            if (_wildPokemon.Count > 0 || _nearbyPokemon.Count > 0)
            {
                await UpdatePokemon().ConfigureAwait(false);
            }
            if (_encounters.Count > 0)
            {
                await UpdateEncounters().ConfigureAwait(false);
            }
            if (_inventory.Count > 0)
            {
                // TODO: Inventory
            }
            if (_playerData.Count > 0)
            {
                await UpdatePlayerData().ConfigureAwait(false);
            }
        }

        private async Task UpdateCells()
        {
            // TODO: Use .Take(MaxConcurrency) instead of looping full list

            ulong now = DateTime.UtcNow.ToTotalSeconds();
            using (IServiceScope scope = _scopeFactory.CreateScope())
            {
                IDbContextFactory<DeviceControllerContext> dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<DeviceControllerContext>>();
                using DeviceControllerContext ctx = dbFactory.CreateDbContext();
                CellRepository repo = new CellRepository(ctx);
                Stopwatch stopwatch = new Stopwatch();
                List<Cell> updatedCells = new List<Cell>();
                stopwatch.Start();
                lock (_cellsLock)
                {
                    //_logger.LogWarning($"Cell count before: {_cells.Count}");
                    int count = Math.Min(MaxConcurrency, _cells.Count);
                    List<ulong> cells = _cells.GetRange(0, count);
                    _cells.RemoveRange(0, count);
                    //_logger.LogWarning($"Cell count after: {_cells.Count}");
                    foreach (ulong cellId in cells)
                    {
                        // NOTE: Can't await within lock body, call awaiter synchronously
                        S2Cell s2cell = new S2Cell(new S2CellId(cellId));
                        S2LatLng center = s2cell.RectBound.Center;
                        updatedCells.Add(new Cell
                        {
                            Id = cellId,
                            Level = s2cell.Level,
                            Latitude = center.LatDegrees,
                            Longitude = center.LngDegrees,
                            Updated = now,
                        });
                        if (!_gymIdsPerCell.ContainsKey(cellId))
                        {
                            _gymIdsPerCell.Add(cellId, new List<string>());
                        }
                        if (!_stopIdsPerCell.ContainsKey(cellId))
                        {
                            _stopIdsPerCell.Add(cellId, new List<string>());
                        }
                    }
                    //_cells.Clear();
                }
                if (updatedCells.Count > 0)
                {
                    await repo.AddOrUpdateAsync(updatedCells).ConfigureAwait(false);
                }

                stopwatch.Stop();
                _logger.LogInformation($"[ConsumerService] S2Cell Count: {updatedCells.Count} parsed in {stopwatch.Elapsed.TotalSeconds}s");
                System.Threading.Thread.Sleep(50);
            }
            await Task.CompletedTask.ConfigureAwait(false);
        }

        /*
        private async Task UpdateSpawnpoints()
        {
            var now = DateTime.UtcNow.ToTotalSeconds();
            using (var scope = _scopeFactory.CreateScope())
            {
                var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<DeviceControllerContext>>();
                using (var ctx = dbFactory.CreateDbContext())
                {
                    var repo = new SpawnpointRepository(ctx);
                    var stopwatch = new Stopwatch();
                    var updatedSpawnpoints = new List<Spawnpoint>();
                    stopwatch.Start();
                    lock (_spawnpointsLock)
                    {
                        foreach (var spawnpoint in _spawnpoints)
                        {
                            spawnpoint.Updated = now;
                            // NOTE: Can't await within lock body, call awaiter synchronously
                            updatedSpawnpoints.Add(spawnpoint);
                        }
                        _spawnpoints.Clear();
                    }
                    if (updatedSpawnpoints.Count > 0)
                    {
                        await repo.AddOrUpdateAsync(updatedSpawnpoints);
                    }

                    stopwatch.Stop();
                    _logger.LogInformation($"[ConsumerService] Spawnpoints Count: {updatedSpawnpoints.Count} parsed in {stopwatch.Elapsed.TotalSeconds}s");
                    System.Threading.Thread.Sleep(50);
                }
            }
            await Task.CompletedTask;
        }
        */

        private async Task UpdateWeather()
        {
            ulong now = DateTime.UtcNow.ToTotalSeconds();
            using (IServiceScope scope = _scopeFactory.CreateScope())
            {
                IDbContextFactory<DeviceControllerContext> dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<DeviceControllerContext>>();
                using DeviceControllerContext ctx = dbFactory.CreateDbContext();
                WeatherRepository repo = new WeatherRepository(ctx);
                Stopwatch stopwatch = new Stopwatch();
                List<Weather> updatedWeather = new List<Weather>();
                stopwatch.Start();
                lock (_weatherLock)
                {
                    foreach (ClientWeatherProto weather in _clientWeather)
                    {
                        S2Cell s2cell = new S2Cell(new S2CellId((ulong)weather.S2CellId));
                        S2LatLng center = s2cell.RectBound.Center;
                        WeatherAlertProto alert = weather.Alerts?.FirstOrDefault();
                        updatedWeather.Add(new Weather
                        {
                            Id = (long)s2cell.Id.Id,
                            Level = s2cell.Level,
                            Latitude = center.LatDegrees,
                            Longitude = center.LngDegrees,
                            GameplayCondition = weather.GameplayWeather.GameplayCondition,
                            WindDirection = (ushort)weather.DisplayWeather.WindDirection,
                            CloudLevel = (ushort)weather.DisplayWeather.CloudLevel,
                            RainLevel = (ushort)weather.DisplayWeather.RainLevel,
                            WindLevel = (ushort)weather.DisplayWeather.WindLevel,
                            SnowLevel = (ushort)weather.DisplayWeather.SnowLevel,
                            FogLevel = (ushort)weather.DisplayWeather.FogLevel,
                            SpecialEffectLevel = (ushort)weather.DisplayWeather.SpecialEffectLevel,
                            Severity = weather.Alerts?.Count > 0 ? (ushort)weather.Alerts?.FirstOrDefault().Severity : null,
                            WarnWeather = alert?.WarnWeather,
                            Updated = now,
                        });
                    }
                    _clientWeather.Clear();
                }
                if (updatedWeather.Count > 0)
                {
                    await repo.AddOrUpdateAsync(updatedWeather).ConfigureAwait(false);
                    WebhookController.Instance.AddWeather(updatedWeather);
                }

                stopwatch.Stop();
                _logger.LogInformation($"[ConsumerService] Weather Count: {updatedWeather.Count} parsed in {stopwatch.Elapsed.TotalSeconds}s");
                System.Threading.Thread.Sleep(50);
            }
            await Task.CompletedTask.ConfigureAwait(false);
        }

        private async Task UpdateForts()
        {
            using (IServiceScope scope = _scopeFactory.CreateScope())
            {
                IDbContextFactory<DeviceControllerContext> dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<DeviceControllerContext>>();
                using DeviceControllerContext ctx = dbFactory.CreateDbContext();
                PokestopRepository pokestopRepository = new PokestopRepository(ctx);
                GymRepository gymRepository = new GymRepository(ctx);
                Stopwatch stopwatch = new Stopwatch();
                List<Pokestop> updatedPokestops = new List<Pokestop>();
                List<Gym> updatedGyms = new List<Gym>();
                stopwatch.Start();
                lock (_fortsLock)
                {
                    int count = Math.Min(MaxConcurrency, _forts.Count);
                    List<dynamic> forts = _forts.GetRange(0, count);
                    _forts.RemoveRange(0, count);
                    foreach (dynamic item in forts)
                    {
                        dynamic cellId = Convert.ToUInt64(item.cell);
                        PokemonFortProto fort = (PokemonFortProto)item.data;
                        switch (fort.FortType)
                        {
                            case FortType.Gym:
                                Gym gym = new Gym(cellId, fort);
                                WebhookController.Instance.AddGym(gym);
                                updatedGyms.Add(gym);
                                if (!_gymIdsPerCell.ContainsKey(cellId))
                                {
                                    _gymIdsPerCell.Add(cellId, new List<string>());
                                }
                                _gymIdsPerCell[cellId].Add(fort.FortId);
                                break;
                            case FortType.Checkpoint:
                                Pokestop pokestop = new Pokestop(cellId, fort);
                                WebhookController.Instance.AddPokestop(pokestop);
                                updatedPokestops.Add(pokestop);
                                if (!_stopIdsPerCell.ContainsKey(cellId))
                                {
                                    _stopIdsPerCell.Add(cellId, new List<string>());
                                }
                                _stopIdsPerCell[cellId].Add(fort.FortId);
                                break;
                        }
                    }
                    //_forts.Clear();
                }
                if (updatedGyms.Count > 0)
                {
                    await gymRepository.AddOrUpdateAsync(updatedGyms, false).ConfigureAwait(false);
                }
                if (updatedPokestops.Count > 0)
                {
                    await pokestopRepository.AddOrUpdateAsync(updatedPokestops, true).ConfigureAwait(false);
                }

                stopwatch.Stop();
                _logger.LogInformation($"[ConsumerService] Forts Count: {updatedGyms.Count + updatedPokestops.Count} parsed in {stopwatch.Elapsed.TotalSeconds}s");
                System.Threading.Thread.Sleep(50);
            }
            await Task.CompletedTask.ConfigureAwait(false);
        }

        private async Task UpdateFortDetails()
        {
            ulong now = DateTime.UtcNow.ToTotalSeconds();
            using (IServiceScope scope = _scopeFactory.CreateScope())
            {
                IDbContextFactory<DeviceControllerContext> dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<DeviceControllerContext>>();
                using DeviceControllerContext ctx = dbFactory.CreateDbContext();
                PokestopRepository pokestopRepository = new PokestopRepository(ctx);
                GymRepository gymRepository = new GymRepository(ctx);
                Stopwatch stopwatch = new Stopwatch();
                List<Pokestop> updatedPokestops = new List<Pokestop>();
                List<Gym> updatedGyms = new List<Gym>();
                stopwatch.Start();
                lock (_fortDetailsLock)
                {
                    foreach (FortDetailsOutProto details in _fortDetails)
                    {
                        switch (details.FortType)
                        {
                            case FortType.Gym:
                                Gym gym = gymRepository.GetByIdAsync(details.Id)
                                                       .ConfigureAwait(false)
                                                       .GetAwaiter()
                                                       .GetResult();
                                if (gym != null)
                                {
                                    gym.AddDetails(details);
                                    gym.Updated = now;
                                    //FirstSeenTimestamp = now,
                                    updatedGyms.Add(gym);
                                }
                                break;
                            case FortType.Checkpoint:
                                Pokestop pokestop = pokestopRepository.GetByIdAsync(details.Id)
                                                                 .ConfigureAwait(false)
                                                                 .GetAwaiter()
                                                                 .GetResult();
                                if (pokestop != null)
                                {
                                    pokestop.AddDetails(details);
                                    pokestop.Updated = now;
                                    //FirstSeenTimestamp = now,
                                    updatedPokestops.Add(pokestop);
                                }
                                break;
                        }
                    }
                    _fortDetails.Clear();
                }
                if (updatedGyms.Count > 0)
                {
                    await gymRepository.AddOrUpdateAsync(updatedGyms, true).ConfigureAwait(false);
                }
                if (updatedPokestops.Count > 0)
                {
                    await pokestopRepository.AddOrUpdateAsync(updatedPokestops, true).ConfigureAwait(false);
                }

                stopwatch.Stop();
                _logger.LogInformation($"[ConsumerServer] FortDetails Count: {updatedGyms.Count + updatedPokestops.Count} parsed in {stopwatch.Elapsed.TotalSeconds}s");
                System.Threading.Thread.Sleep(50);
            }
            await Task.CompletedTask.ConfigureAwait(false);
        }

        private async Task UpdateGymGetInfo()
        {
            //var now = DateTime.UtcNow.ToTotalSeconds();
            using (IServiceScope scope = _scopeFactory.CreateScope())
            {
                IDbContextFactory<DeviceControllerContext> dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<DeviceControllerContext>>();
                using DeviceControllerContext ctx = dbFactory.CreateDbContext();
                GymRepository gymRepository = new GymRepository(ctx);
                TrainerRepository trainerRepository = new TrainerRepository(ctx);
                GymDefenderRepository gymDefenderRepository = new GymDefenderRepository(ctx);
                Stopwatch stopwatch = new Stopwatch();
                List<Gym> updatedGyms = new List<Gym>();
                List<GymDefender> updatedDefenders = new List<GymDefender>();
                List<Trainer> updatedTrainers = new List<Trainer>();
                stopwatch.Start();
                lock (_gymInfosLock)
                {
                    foreach (GymGetInfoOutProto gymInfo in _gymInfos)
                    {
                        if (gymInfo.GymStatusAndDefenders == null)
                        {
                            _logger.LogWarning($"[ConsumerService] Invalid GymStatusAndDefenders provided, skipping...\n: {gymInfo}");
                            continue;
                        }
                        string id = gymInfo.GymStatusAndDefenders.PokemonFortProto.FortId;
                        Google.Protobuf.Collections.RepeatedField<GymDefenderProto> gymDefenders = gymInfo.GymStatusAndDefenders.GymDefender;

                        foreach (GymDefenderProto gymDefender in gymDefenders)
                        {
                            PlayerPublicProfileProto trainerProfile = gymDefender.TrainerPublicProfile;
                            updatedTrainers.Add(new Trainer
                            {
                                Name = trainerProfile.Name,
                                Level = (ushort)trainerProfile.Level,
                                TeamId = (ushort)trainerProfile.Team,
                                BattlesWon = (uint)trainerProfile.BattlesWon,
                                KmWalked = trainerProfile.KmWalked,
                                PokemonCaught = (ulong)trainerProfile.CaughtPokemon,
                                Experience = (ulong)trainerProfile.Experience,
                                CombatRank = (ulong)trainerProfile.CombatRank,
                                CombatRating = trainerProfile.CombatRating,
                            });

                            MotivatedPokemonProto defenderPokemon = gymDefender.MotivatedPokemon;
                            updatedDefenders.Add(new GymDefender
                            {
                                Id = defenderPokemon.Pokemon.Id.ToString(), // TODO: Convert to ulong
                                PokemonId = (ushort)defenderPokemon.Pokemon.PokemonId,
                                CpWhenDeployed = (uint)defenderPokemon.CpWhenDeployed,
                                CpNow = (uint)defenderPokemon.CpNow,
                                BerryValue = defenderPokemon.BerryValue,
                                TimesFed = (ushort)gymDefender.DeploymentTotals.TimesFed,
                                DeploymentDuration = (uint)gymDefender.DeploymentTotals.DeploymentDurationMs / 1000,
                                TrainerName = defenderPokemon.Pokemon.OwnerName,
                                // original_owner_name
                                FortId = id,
                                AttackIV = (ushort)defenderPokemon.Pokemon.IndividualAttack,
                                DefenseIV = (ushort)defenderPokemon.Pokemon.IndividualDefense,
                                StaminaIV = (ushort)defenderPokemon.Pokemon.IndividualStamina,
                                Move1 = (ushort)defenderPokemon.Pokemon.Move1,
                                Move2 = (ushort)defenderPokemon.Pokemon.Move2,
                                BattlesAttacked = (ushort)defenderPokemon.Pokemon.BattlesAttacked,
                                BattlesDefended = (ushort)defenderPokemon.Pokemon.BattlesDefended,
                                Gender = (ushort)defenderPokemon.Pokemon.PokemonDisplay.Gender,
                                // form
                                HatchedFromEgg = defenderPokemon.Pokemon.HatchedFromEgg,
                                PvpCombatWon = (ushort)defenderPokemon.Pokemon.PvpCombatStats.NumWon,
                                PvpCombatTotal = (ushort)defenderPokemon.Pokemon.PvpCombatStats.NumTotal,
                                NpcCombatWon = (ushort)defenderPokemon.Pokemon.NpcCombatStats.NumWon,
                                NpcCombatTotal = (ushort)defenderPokemon.Pokemon.NpcCombatStats.NumTotal,
                            });
                        }

                        Gym gym = gymRepository.GetByIdAsync(id)
                                               .ConfigureAwait(false)
                                               .GetAwaiter()
                                               .GetResult();
                        if (gym != null)
                        {
                            gym.AddDetails(gymInfo);
                            updatedGyms.Add(gym);
                        }
                    }
                    _fortDetails.Clear();
                }
                if (updatedGyms.Count > 0)
                {
                    await gymRepository.AddOrUpdateAsync(updatedGyms, true).ConfigureAwait(false);
                    await trainerRepository.AddOrUpdateAsync(updatedTrainers).ConfigureAwait(false);
                    await gymDefenderRepository.AddOrUpdateAsync(updatedDefenders).ConfigureAwait(false);
                    // TODO: Webhooks
                }

                stopwatch.Stop();
                _logger.LogInformation($"[ConsumerServer] GymInfo Count: {updatedGyms.Count} parsed in {stopwatch.Elapsed.TotalSeconds}s");
                System.Threading.Thread.Sleep(50);
            }
            await Task.CompletedTask.ConfigureAwait(false);
        }

        private async Task UpdatePokemon()
        {
            //var now = DateTime.UtcNow.ToTotalSeconds();
            using (IServiceScope scope = _scopeFactory.CreateScope())
            {
                IDbContextFactory<DeviceControllerContext> dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<DeviceControllerContext>>();
                using DeviceControllerContext ctx = dbFactory.CreateDbContext();
                PokemonRepository pokemonRepository = new PokemonRepository(ctx);
                PokestopRepository pokestopRepository = new PokestopRepository(ctx);
                Stopwatch stopwatch = new Stopwatch();
                List<Pokemon> updatedPokemon = new List<Pokemon>();
                stopwatch.Start();
                lock (_wildPokemonLock)
                {
                    if (_wildPokemon.Count > 0)
                    {
                        int count = Math.Min(MaxConcurrency, _wildPokemon.Count);
                        List<dynamic> wild = _wildPokemon.GetRange(0, count);
                        _wildPokemon.RemoveRange(0, count);
                        foreach (dynamic item in wild)
                        {
                            ulong cell = (ulong)item.cell;
                            WildPokemonProto wildPokemon = (WildPokemonProto)item.data;
                            ulong timestampMs = (ulong)item.timestamp_ms;
                            dynamic username = item.username;
                            ulong id = wildPokemon.EncounterId;
                            Pokemon pokemon = new Pokemon(wildPokemon, cell, timestampMs, username, false); // TODO: IsEvent
                            Pokemon oldPokemon = pokemonRepository.GetByIdAsync(pokemon.Id)
                                                              .ConfigureAwait(false)
                                                              .GetAwaiter()
                                                              .GetResult();
                            pokemon.Update(oldPokemon);
                            updatedPokemon.Add(pokemon);
                            InstanceController.Instance.GotPokemon(pokemon);
                            // TODO: Pokemon.Save();
                        }
                        //_wildPokemon.Clear();
                    }
                }
                lock (_nearbyPokemonLock)
                {
                    if (_nearbyPokemon.Count > 0)
                    {
                        int count = Math.Min(MaxConcurrency, _nearbyPokemon.Count);
                        List<dynamic> nearby = _nearbyPokemon.GetRange(0, count);
                        _nearbyPokemon.RemoveRange(0, count);
                        foreach (dynamic item in nearby)
                        {
                            ulong cell = (ulong)item.cell;
                            // data.timestamp_ms
                            NearbyPokemonProto nearbyPokemon = (NearbyPokemonProto)item.data;
                            dynamic username = item.username;
                            Pokemon pokemon = new Pokemon(nearbyPokemon, cell, username, false); // TODO: IsEvent
                            if (pokemon.Latitude == 0 && string.IsNullOrEmpty(pokemon.PokestopId))
                            {
                                // Skip nearby pokemon without pokestop id set and no coordinate
                                continue;
                            }
                            Pokestop pokestop = pokestopRepository.GetByIdAsync(pokemon.PokestopId)
                                                             .ConfigureAwait(false)
                                                             .GetAwaiter()
                                                             .GetResult();
                            if (pokestop == null)
                            {
                                // Unknown stop, skip pokemon
                                continue;
                            }
                            pokemon.Latitude = pokestop.Latitude;
                            pokemon.Longitude = pokestop.Longitude;
                            Pokemon oldPokemon = pokemonRepository.GetByIdAsync(pokemon.Id)
                                                              .ConfigureAwait(false)
                                                              .GetAwaiter()
                                                              .GetResult();
                            pokemon.Update(oldPokemon);
                            updatedPokemon.Add(pokemon);
                            InstanceController.Instance.GotPokemon(pokemon);
                            // TODO: Pokemon.Save();
                        }
                        //_nearbyPokemon.Clear();
                    }
                }
                if (updatedPokemon.Count > 0)
                {
                    await pokemonRepository.AddOrUpdateAsync(updatedPokemon, false, true).ConfigureAwait(false);
                }

                stopwatch.Stop();
                _logger.LogInformation($"[ConsumerService] Wild/Nearby Pokemon Count: {updatedPokemon.Count} parsed in {stopwatch.Elapsed.TotalSeconds}s");
                System.Threading.Thread.Sleep(50);
            }
            await Task.CompletedTask.ConfigureAwait(false);
        }

        private async Task UpdateEncounters()
        {
            using (IServiceScope scope = _scopeFactory.CreateScope())
            {
                IDbContextFactory<DeviceControllerContext> dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<DeviceControllerContext>>();
                using DeviceControllerContext ctx = dbFactory.CreateDbContext();
                PokemonRepository pokemonRepository = new PokemonRepository(ctx);
                Stopwatch stopwatch = new Stopwatch();
                List<Pokemon> updatedPokemon = new List<Pokemon>();
                stopwatch.Start();
                lock (_encountersLock)
                {
                    foreach (dynamic item in _encounters)
                    {
                        EncounterOutProto encounter = (EncounterOutProto)item.encounter;
                        dynamic username = item.username;
                        Pokemon pokemon;
                        try
                        {
                            pokemon = pokemonRepository.GetByIdAsync(encounter.Pokemon.EncounterId.ToString()) // TODO: is_event
                                                       .ConfigureAwait(false)
                                                       .GetAwaiter()
                                                       .GetResult();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"Error: {ex}");
                            pokemon = null;
                        }
                        if (pokemon != null)
                        {
                            pokemon.AddEncounter(encounter, username)
                                   .ConfigureAwait(false)
                                   .GetAwaiter()
                                   .GetResult();
                            pokemon.Update(pokemon, true);
                            updatedPokemon.Add(pokemon);
                            InstanceController.Instance.GotIV(pokemon);
                        }
                        else
                        {
                            Coordinate centerCoord = new Coordinate(encounter.Pokemon.Latitude, encounter.Pokemon.Longitude);
                            S2CellId cellId = S2CellId.FromLatLng(S2LatLng.FromDegrees(centerCoord.Latitude, centerCoord.Longitude));
                            ulong timestampMs = DateTime.UtcNow.ToTotalSeconds() * 1000;
                            Pokemon newPokemon = new Pokemon(encounter.Pokemon, cellId.Id, timestampMs, username, false); // TODO: IsEvent
                            newPokemon.AddEncounter(encounter, username)
                                      .ConfigureAwait(false)
                                      .GetAwaiter()
                                      .GetResult();
                            updatedPokemon.Add(newPokemon);
                            InstanceController.Instance.GotIV(newPokemon);
                        }
                    }
                    _encounters.Clear();
                }

                if (updatedPokemon.Count > 0)
                {
                    await pokemonRepository.AddOrUpdateAsync(updatedPokemon, false, true).ConfigureAwait(false);
                }

                stopwatch.Stop();
                _logger.LogInformation($"[ConsumerService] Encounter Count: {updatedPokemon.Count} parsed in {stopwatch.Elapsed.TotalSeconds}s");
                System.Threading.Thread.Sleep(50);
            }
            await Task.CompletedTask.ConfigureAwait(false);
        }

        private async Task UpdateQuests()
        {
            using (IServiceScope scope = _scopeFactory.CreateScope())
            {
                IDbContextFactory<DeviceControllerContext> dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<DeviceControllerContext>>();
                using DeviceControllerContext ctx = dbFactory.CreateDbContext();
                PokestopRepository pokestopRepository = new PokestopRepository(ctx);
                Stopwatch stopwatch = new Stopwatch();
                List<Pokestop> updatedQuests = new List<Pokestop>();
                stopwatch.Start();
                lock (_questsLock)
                {
                    foreach (QuestProto quest in _quests)
                    {
                        // Get existing pokestop, and add quest to it
                        Pokestop pokestop = pokestopRepository.GetByIdAsync(quest.FortId)
                                                         .ConfigureAwait(false)
                                                         .GetAwaiter()
                                                         .GetResult();
                        // Skip quests we don't have stops for yet
                        if (pokestop == null)
                        {
                            continue;
                        }
                        /*
if (await pokestop.TriggerWebhook(true))
{
   _logger.LogDebug($"[Quest] Found a quest belonging to a new stop, skipping..."); // :face_with_raised_eyebrow:
   continue;
}
*/
                        pokestop.AddQuest(quest);
                        updatedQuests.Add(pokestop);
                    }
                    _quests.Clear();
                }

                if (updatedQuests.Count > 0)
                {
                    await pokestopRepository.AddOrUpdateAsync(updatedQuests, false).ConfigureAwait(false);
                    WebhookController.Instance.AddQuests(updatedQuests);
                }

                stopwatch.Stop();
                _logger.LogInformation($"[ConsumerService] Quest Count: {updatedQuests.Count} parsed in {stopwatch.Elapsed.TotalSeconds}s");
                System.Threading.Thread.Sleep(50);
            }
            await Task.CompletedTask.ConfigureAwait(false);
        }

        private async Task UpdatePlayerData()
        {
            ulong now = DateTime.UtcNow.ToTotalSeconds();
            using (IServiceScope scope = _scopeFactory.CreateScope())
            {
                IDbContextFactory<DeviceControllerContext> dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<DeviceControllerContext>>();
                using DeviceControllerContext ctx = dbFactory.CreateDbContext();
                AccountRepository accountRepository = new AccountRepository(ctx);
                Stopwatch stopwatch = new Stopwatch();
                List<Account> updatedAccounts = new List<Account>();
                stopwatch.Start();
                lock (_playerDataLock)
                {
                    foreach (dynamic item in _playerData)
                    {
                        dynamic username = item.username;
                        GetPlayerOutProto playerData = (GetPlayerOutProto)item.gpr;
                        // Get account
                        dynamic account = accountRepository.GetByIdAsync(username)
                                                       .ConfigureAwait(false)
                                                       .GetAwaiter()
                                                       .GetResult();
                        // Skip account if we failed to get it
                        if (account == null)
                        {
                            continue;
                        }

                        account.CreationTimestamp = (ulong)playerData.Player.CreationTimeMs / 1000;
                        account.Warn = playerData.Warn;
                        ulong warnExpireTimestamp = (ulong)playerData.WarnExpireMs / 1000;
                        if (warnExpireTimestamp > 0)
                        {
                            account.WarnExpireTimestamp = warnExpireTimestamp;
                        }
                        account.WarnMessageAcknowledged = playerData.WarnMessageAcknowledged;
                        account.SuspendedMessageAcknowledged = playerData.SuspendedMessageAcknowledged;
                        account.WasSuspended = playerData.WasSuspended;
                        account.Banned = playerData.Banned;
                        if (playerData.Warn && string.IsNullOrEmpty(account.Failed))
                        {
                            account.Failed = "GPR_RED_WARNING";
                            if (account.FirstWarningTimestamp == null)
                            {
                                account.FirstWarningTimestamp = now;
                            }
                            account.FailedTimestamp = now;
                            _logger.LogWarning($"[ConsumerService] Account {account.Username}|{playerData.Player.Name} - Red Warning: {playerData.Banned}");
                        }
                        if (playerData.Banned)
                        {
                            account.Failed = "GPR_BANNED";
                            account.FailedTimestamp = now;
                            _logger.LogWarning($"[ConsumerService] Account {account.Username}|{playerData.Player.Name} - Banned: {playerData.Banned}");
                        }
                        updatedAccounts.Add(account);
                    }
                    _playerData.Clear();
                }

                if (updatedAccounts.Count > 0)
                {
                    // TODO: Ignore gpr warn/ban columns if overwriting
                    await accountRepository.AddOrUpdateAsync(updatedAccounts).ConfigureAwait(false);
                }

                stopwatch.Stop();
                _logger.LogInformation($"[ConsumerService] Account Count: {updatedAccounts.Count} parsed in {stopwatch.Elapsed.TotalSeconds}s");
                System.Threading.Thread.Sleep(50);
            }
            await Task.CompletedTask.ConfigureAwait(false);
        }

        #endregion
    }
}