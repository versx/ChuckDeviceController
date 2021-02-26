namespace ChuckDeviceController.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;

    using Google.Common.Geometry;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;
    using POGOProtos.Rpc;
    using StackExchange.Redis;

    using Chuck.Infrastructure.Common;
    using Chuck.Infrastructure.Data.Contexts;
    using Chuck.Infrastructure.Data.Entities;
    using Chuck.Infrastructure.Data.Factories;
    using Chuck.Infrastructure.Data.Interfaces;
    using Chuck.Infrastructure.Data.Repositories;
    using Chuck.Infrastructure.Extensions;
    using Chuck.Infrastructure.JobControllers;
    using ChuckDeviceController.Models.Requests;
    using ChuckDeviceController.Models.Responses;
    using ChuckDeviceController.Services;
    using ChuckDeviceController.Services.Models;

    [ApiController]
    public class ProtoController : ControllerBase
    {
        private const string RedisQueueName = "*";

        #region Variables

        private readonly IConnectionMultiplexer _redis;
        private readonly ISubscriber _subscriber;

        private readonly AccountRepository _accountRepository;
        private readonly DeviceRepository _deviceRepository;
        private readonly GymRepository _gymRepository;
        private readonly GymDefenderRepository _gymDefenderRepository;
        private readonly TrainerRepository _trainerRepository;
        private readonly PokemonRepository _pokemonRepository;
        private readonly PokestopRepository _pokestopRepository;
        private readonly SpawnpointRepository _spawnpointRepository;
        private readonly CellRepository _cellRepository;
        private readonly WeatherRepository _weatherRepository;

        //private readonly DeviceControllerContext _context;
        //private readonly IDbContextFactory<DeviceControllerContext> _dbFactory;
        private readonly IConsumerService _consumerService;
        private readonly ILogger<ProtoController> _logger;

        private readonly Dictionary<ulong, int> _emptyCells;
        private readonly Dictionary<string, ushort> _levelCache;

        private readonly Dictionary<ulong, List<string>> _gymIdsPerCell;
        private readonly Dictionary<ulong, List<string>> _stopIdsPerCell;

        #endregion

        #region Constructor

        public ProtoController(IDbContextFactory<DeviceControllerContext> dbFactory, IConsumerService consumerService, IConnectionMultiplexer connectionMultiplexer, ILogger<ProtoController> logger)
        {
            //_context = context;
            //_dbFactory = dbFactory;
            _consumerService = consumerService;
            _redis = connectionMultiplexer;
            _subscriber = _redis.GetSubscriber();
            _logger = logger;

            _accountRepository = new AccountRepository(dbFactory.CreateDbContext());
            _deviceRepository = new DeviceRepository(dbFactory.CreateDbContext());
            _gymRepository = new GymRepository(dbFactory.CreateDbContext());
            _gymDefenderRepository = new GymDefenderRepository(dbFactory.CreateDbContext());
            _trainerRepository = new TrainerRepository(dbFactory.CreateDbContext());
            _pokemonRepository = new PokemonRepository(dbFactory.CreateDbContext());
            _pokestopRepository = new PokestopRepository(dbFactory.CreateDbContext());
            _cellRepository = new CellRepository(dbFactory.CreateDbContext());
            _weatherRepository = new WeatherRepository(dbFactory.CreateDbContext());
            _spawnpointRepository = new SpawnpointRepository(dbFactory.CreateDbContext());

            _emptyCells = new Dictionary<ulong, int>();
            _levelCache = new Dictionary<string, ushort>();
            _gymIdsPerCell = new Dictionary<ulong, List<string>>();
            _stopIdsPerCell = new Dictionary<ulong, List<string>>();
        }

        #endregion

        #region Routes

        [HttpGet("/raw")]
        public string Get() => ":D";

        // Handle RDM data
        [
            HttpPost("/raw"),
            Produces("application/json"),
        ]
        public async Task<ProtoResponse> PostAsync(ProtoPayload payload)
        {
            var response = await HandleProtoRequest(payload).ConfigureAwait(false);
            if (response?.Data == null)
            {
                _logger.LogError($"[Proto] [{payload.Uuid}] null data response!");
            }
            Response.Headers["Accept"] = "application/json";
            Response.Headers["Content-Type"] = "application/json";
            return response;
        }

        // Handle MAD data
        /*
        [
            HttpPost("/raw"),
            Produces("application/json"),
        ]
        public async Task<ProtoResponse> PostAsync(List<ProtoData> payloads)
        {
            Response.Headers["Accept"] = "application/json";
            Response.Headers["Content-Type"] = "application/json";
            var response await HandleProtoRequest(new ProtoPayload
            {
                Username = "PogoDroid",
                Uuid = Request.Headers["Origin"],
                Contents = payloads,
            });
            return response;
        }
        */

        #endregion

        #region Handlers

        private async Task<ProtoResponse> HandleProtoRequest(ProtoPayload payload)
        {
            if (payload == null)
            {
                _logger.LogError("Invalid proto payload received");
                return null;
            }

            var device = await _deviceRepository.GetByIdAsync(payload.Uuid).ConfigureAwait(false);
            if (device != null)
            {
                device.LastLatitude = payload.LatitudeTarget;
                device.LastLongitude = payload.LongitudeTarget;
                device.LastSeen = DateTime.UtcNow.ToTotalSeconds();
                await _deviceRepository.UpdateAsync(device).ConfigureAwait(false);
            }

            if (!string.IsNullOrEmpty(payload.Username) && payload.Level > 0)
            {
                if (!_levelCache.ContainsKey(payload.Username))
                {
                    _levelCache.Add(payload.Username, payload.Level);
                }
                else
                {
                    var oldLevel = _levelCache[payload.Username];
                    if (oldLevel != payload.Level)
                    {
                        var account = await _accountRepository.GetByIdAsync(payload.Username).ConfigureAwait(false);
                        if (account != null)
                        {
                            account.Level = payload.Level;
                            await _accountRepository.UpdateAsync(account).ConfigureAwait(false);
                        }
                        _levelCache[payload.Username] = payload.Level;
                    }
                }
            }
            if (payload.Contents?.Count == 0)
            {
                _logger.LogWarning($"[Proto] [{payload.Uuid}] Invalid GMO");
                return null;
            }
            var wildPokemon = new List<dynamic>();
            var nearbyPokemon = new List<dynamic>();
            var clientWeather = new List<ClientWeatherProto>();
            var forts = new List<dynamic>();
            var fortDetails = new List<FortDetailsOutProto>();
            var gymInfos = new List<GymGetInfoOutProto>();
            var quests = new List<QuestProto>();
            var fortSearch = new List<FortSearchOutProto>();
            var encounters = new List<dynamic>();
            var cells = new List<ulong>();
            var inventory = new List<InventoryDeltaProto>();
            var playerData = new List<dynamic>();
            //var spawnpoints = new List<Spawnpoint>();

            var isEmptyGmo = true;
            var isInvalidGmo = true;
            var containsGmo = false;

            if (payload.Contents == null)
            {
                _logger.LogWarning($"[Proto] [{payload.Uuid}] Empty data");
                return null;
            }

            Coordinate targetCoord = null;
            var inArea = false;
            if (payload.LatitudeTarget != 0 && payload.LongitudeTarget != 0)
            {
                targetCoord = new Coordinate(payload.LatitudeTarget, payload.LongitudeTarget);
            }
            var targetKnown = false;
            S2CellId targetCellId = default;
            if (targetCoord != null)
            {
                // Check target is within cell id instead of checking geofences
                targetKnown = true;
                targetCellId = S2CellId.FromLatLng(S2LatLng.FromDegrees(targetCoord.Latitude, targetCoord.Longitude));
                //_logger.LogDebug($"[Proto] [{payload.Uuid}] Data received within target area {targetCoord} and target distance {payload.TargetMaxDistance}");
            }
            //_logger.LogWarning($"[{device.Uuid}] InArea={inArea}");

            foreach (var rawData in payload.Contents)
            {
                if (string.IsNullOrEmpty(rawData.Data))
                {
                    _logger.LogWarning($"[Proto] [{payload.Uuid}] Unhandled proto {rawData.Method}: {rawData.Data}");
                    continue;
                }
                var data = rawData.Data;
                var method = (Method)rawData.Method;
                switch (method)
                {
                    case Method.GetPlayer:
                        try
                        {
                            var gpr = GetPlayerOutProto.Parser.ParseFrom(Convert.FromBase64String(data));
                            if (gpr?.Success == true)
                            {
                                playerData.Add(new
                                {
                                    gpr,
                                    username = payload.Username,
                                });
                            }
                            else
                            {
                                _logger.LogError($"[Proto] [{payload.Uuid}] Malformed GetPlayerOutProto");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"[Proto] [{payload.Uuid}] Unable to decode GetPlayerOutProto: {ex}");
                        }
                        break;
                    case Method.GetHoloholoInventory:
                        try
                        {
                            var ghi = GetHoloholoInventoryOutProto.Parser.ParseFrom(Convert.FromBase64String(data));
                            if (ghi?.Success == true)
                            {
                                if (ghi.InventoryDelta.InventoryItem?.Count > 0)
                                {
                                    inventory.Add(ghi.InventoryDelta);
                                }
                            }
                            else
                            {
                                _logger.LogError($"[Proto] [{payload.Uuid}] Malformed GetHoloholoInventoryOutProto");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"[Proto] [{payload.Uuid}] Unable to decode GetHoloholoInventoryOutProto: {ex}");
                        }
                        break;
                    case Method.FortSearch:
                        try
                        {
                            var fsr = FortSearchOutProto.Parser.ParseFrom(Convert.FromBase64String(data));
                            if (fsr != null)
                            {
                                if (fsr.ChallengeQuest?.Quest != null)
                                {
                                    //quests.Add(fsr.ChallengeQuest.Quest);
                                    await PublishData(RedisChannels.ProtoQuest, fsr.ChallengeQuest.Quest);
                                }
                                fortSearch.Add(fsr);
                            }
                            else
                            {
                                _logger.LogError($"[Proto] [{payload.Uuid}] Malformed FortSearchOutProto");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"[Proto] [{payload.Uuid}] Unable to decode FortSearchOutProto: {ex}");
                        }
                        break;
                    case Method.Encounter:
                        isEmptyGmo = false;
                        isInvalidGmo = false;
                        try
                        {
                            if (payload.Level >= 30)
                            {
                                var er = EncounterOutProto.Parser.ParseFrom(Convert.FromBase64String(data));
                                if (er?.Status == EncounterOutProto.Types.Status.EncounterSuccess)
                                {
                                    //encounters.Add(new
                                    await PublishData(RedisChannels.ProtoEncounter, new
                                    {
                                        data = er,
                                        username = payload.Username,
                                    });
                                }
                                else if (er == null)
                                {
                                    _logger.LogError($"[Proto] [{payload.Uuid}] Malformed EncounterOutProto");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"[Proto] [{payload.Uuid}] Unable to decode EncounterOutProto: {ex}");
                        }
                        break;
                    case Method.FortDetails:
                        try
                        {
                            var fdr = FortDetailsOutProto.Parser.ParseFrom(Convert.FromBase64String(data));
                            if (fdr != null)
                            {
                                fortDetails.Add(fdr);
                                //await PublishData(RedisChannels.Fort, fdr);
                            }
                            else
                            {
                                _logger.LogError($"[Proto] [{payload.Uuid}] Malformed FortDetailsOutProto");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"[Proto] [{payload.Uuid}] Unable to decode FortDetailsOutProto: {ex}");
                        }
                        break;
                    case Method.GetMapObjects:
                        containsGmo = true;
                        try
                        {
                            var gmo = GetMapObjectsOutProto.Parser.ParseFrom(Convert.FromBase64String(data));
                            if (gmo != null)
                            {
                                isInvalidGmo = false;
                                var mapCellsNew = gmo.MapCell;

                                if (mapCellsNew.Count == 0)
                                {
                                    //_logger.LogDebug($"[Proto] [{payload.Uuid}] Map cells are empty");
                                    //return null;
                                }

                                // Check if we're within the same cell, if so then we are within the target distance
                                if (!inArea && targetKnown && mapCellsNew.Select(x => x.S2CellId).Contains(targetCellId.Id))
                                {
                                    inArea = true;
                                }

                                foreach (var mapCell in mapCellsNew)
                                {
                                    //cells.Add(mapCell.S2CellId);
                                    await PublishData(RedisChannels.ProtoCell, mapCell.S2CellId);

                                    var tsMs = mapCell.AsOfTimeMs;
                                    foreach (var wild in mapCell.WildPokemon)
                                    {
                                        // TODO: Send to redis
                                        //wildPokemon.Add(new
                                        await PublishData(RedisChannels.ProtoWildPokemon, new
                                        {
                                            cell = mapCell.S2CellId,
                                            data = wild,
                                            timestamp_ms = tsMs,
                                            username = payload.Username,
                                        });
                                    }
                                    foreach (var nearby in mapCell.NearbyPokemon)
                                    {
                                        //nearbyPokemon.Add(new
                                        await PublishData(RedisChannels.ProtoNearbyPokemon, new
                                        {
                                            cell = mapCell.S2CellId,
                                            data = nearby,
                                            timestamp_ms = tsMs,
                                            username = payload.Username,
                                        });
                                    }
                                    foreach (var fort in mapCell.Fort)
                                    {
                                        //forts.Add(new
                                        await PublishData(RedisChannels.ProtoFort, new
                                        {
                                            cell = mapCell.S2CellId,
                                            data = fort,
                                        });
                                    }
                                }
                                foreach (var weather in gmo.ClientWeather)
                                {
                                    //clientWeather.Add(weather);
                                    await PublishData(RedisChannels.ProtoWeather, weather);
                                }
                                if (wildPokemon.Count == 0 && nearbyPokemon.Count == 0 && forts.Count == 0 && quests.Count == 0)
                                {
                                    foreach (var cellId in cells)
                                    {
                                        if (!_emptyCells.ContainsKey(cellId))
                                        {
                                            _emptyCells.Add(cellId, 1);
                                        }
                                        else
                                        {
                                            _emptyCells[cellId]++;
                                        }
                                        if (_emptyCells[cellId] == 3)
                                        {
                                            _logger.LogWarning($"[Proto] [{payload.Uuid}] Cell {cellId} was empty 3 times in a row, assuming empty...");
                                            //cells.Add(cell);
                                            await PublishData(RedisChannels.ProtoCell, cellId);
                                        }
                                    }
                                    //_logger.LogDebug($"[Proto] [{payload.Uuid}] GMO is empty");
                                    isEmptyGmo = true;
                                }
                                else
                                {
                                    cells.ForEach(cellId => _emptyCells[cellId] = 0);
                                    isEmptyGmo = false;
                                }
                            }
                            else
                            {
                                _logger.LogError($"[Proto] [{payload.Uuid}] Malformed GetMapObjectsOutProto");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"[Proto] [{payload.Uuid}] Unable to decode GetMapObjectsOutProto: {ex}");
                        }
                        break;
                    case Method.GymGetInfo:
                        try
                        {
                            var ggi = GymGetInfoOutProto.Parser.ParseFrom(Convert.FromBase64String(data));
                            if (ggi != null)
                            {
                                gymInfos.Add(ggi);
                            }
                            else
                            {
                                _logger.LogError($"[Proto] [{payload.Uuid}] Malformed GymGetInfoOutProto");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"[Proto] [{payload.Uuid}] Unable to decode GymGetInfoOutProto: {ex}");
                        }
                        break;
                    //case Method.Unset:
                    default:
                        _logger.LogDebug($"[Proto] [{payload.Uuid}] Invalid method or data provided. {method}:{data}");
                        break;
                }
            }

            Coordinate pokemonCoords = null;
            /*
            if (targetCoord != null)
            {
                foreach (var fort in forts)
                {
                    // TODO: InstanceController.Instance.GotFortData(fort.data, payload.Username);
                    if (!inArea)
                    {
                        var coord = new Coordinate(fort.data.Latitude, fort.data.Longitude);
                        if (coord.DistanceTo(targetCoord) <= payload.TargetMaxDistance)
                        {
                            inArea = true;
                        }
                    }
                }
            }
            if (targetCoord != null || payload.PokemonEncounterId != null)
            {
                foreach (var pokemon in wildPokemons)
                {
                    WildPokemonProto wild = (WildPokemonProto)pokemon.data;
                    if (targetCoord != null)
                    {
                        if (pokemonCoords != null && inArea)
                        {
                            break;
                        }

                        if (!inArea)
                        {
                            var coord = new Coordinate(wild.Latitude, wild.Longitude);
                            if (coord.DistanceTo(targetCoord) <= payload.TargetMaxDistance)
                            {
                                inArea = true;
                            }
                        }
                    }
                    if (!string.IsNullOrEmpty(payload.PokemonEncounterId))
                    {
                        if (pokemonCoords != null && inArea)
                        {
                            break;
                        }

                        if (pokemonCoords == null)
                        {
                            if (string.Compare(wild.EncounterId.ToString(), payload.PokemonEncounterId, true) == 0)
                            {
                                pokemonCoords = new Coordinate(wild.Latitude, wild.Longitude);
                            }
                        }
                    }
                }
            }
            if (targetCoord != null && !inArea)
            {
                foreach (var cell in cells)
                {
                    if (inArea)
                    {
                        break;
                    }

                    var s2cell = new S2Cell(new S2CellId(cell));
                    var latlng = new S2LatLng(s2cell.Center);
                    var coord = new Coordinate(latlng.LatDegrees, latlng.LngDegrees);
                    if (coord.DistanceTo(targetCoord) <= Math.Max(payload.TargetMaxDistance ?? 250, 100))
                    {
                        inArea = true;
                    }
                }
            }
            */

            // Loop encounters/wild pokemon, parse spawnpoints, add to list, include in ConsumerData
            /*
            foreach (var item in wildPokemon)
            {
                var wild = (WildPokemonProto)item.data;
                var spawnId = Convert.ToUInt64(wild.SpawnPointId, 16);
                var timestampMs = (ulong)item.timestamp_ms;
                var (verifiedExpireTimestamp, expireTimestamp, spawnpoint) = await HandleSpawnpoint(
                    spawnId, wild.Latitude, wild.Longitude,
                    wild.TimeTillHiddenMs, timestampMs
                );
                if (spawnpoint == null)
                    continue;
                spawnpoints.Add(spawnpoint);
            }
            foreach (var item in encounters)
            {
                var encounter = item.encounter;
                var spawnId = Convert.ToUInt64(encounter.Pokemon.SpawnPointId, 16);
                var timestampMs = DateTime.UtcNow.ToTotalSeconds();
                var (verifiedExpireTimestamp, expireTimestamp, spawnpoint) = await HandleSpawnpoint(
                    spawnId, encounter.Pokemon.Latitude, encounter.Pokemon.Longitude,
                    encounter.Pokemon.TimeTillHiddenMs, timestampMs
                );
                if (spawnpoint == null)
                    continue;
                spawnpoints.Add(spawnpoint);
            }
            */

            var total = wildPokemon.Count + nearbyPokemon.Count + clientWeather.Count +
                forts.Count + fortDetails.Count + gymInfos.Count +
                quests.Count + encounters.Count + cells.Count +
                /*spawnpoints.Count +*/ inventory.Count + playerData.Count;
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            if (targetKnown) // TODO: && inArea)
            {
                var consumerData = new ConsumerData
                {
                    WildPokemon = wildPokemon,
                    NearbyPokemon = nearbyPokemon,
                    ClientWeather = clientWeather,
                    Forts = forts,
                    FortDetails = fortDetails,
                    GymInfo = gymInfos,
                    Quests = quests,
                    FortSearch = fortSearch,
                    Encounters = encounters,
                    Cells = cells,
                    //Spawnpoints = spawnpoints,
                    Inventory = inventory,
                    PlayerData = playerData,
                    Username = payload.Username,
                    Uuid = payload.Uuid,
                };
                /*if (!ThreadPool.QueueUserWorkItem(async (x) =>*/
                //await HandleConsumables(consumerData);
                /*
                {
                    _logger.LogError($"[Proto] [{payload.Uuid}] Failed to queue user work item");
                };
                */
                //await _consumerService.AddData(consumerData).ConfigureAwait(false);
            }

            stopwatch.Stop();
            if (total > 0)
            {
                _logger.LogInformation($"[Proto] [{payload.Uuid}] Update count: {total} parsed in {stopwatch.Elapsed.TotalSeconds}s");
            }
            return new ProtoResponse
            {
                Status = "ok",
                Data = new ProtoDataDetails
                {
                    Nearby = nearbyPokemon.Count,
                    Wild = wildPokemon.Count,
                    Forts = forts.Count,
                    Quests = quests.Count,
                    FortSearch = fortSearch.Count,
                    Encounters = encounters.Count,
                    Level = payload.Level,
                    OnlyEmptyGmos = containsGmo && isEmptyGmo,
                    OnlyInvalidGmos = containsGmo && isInvalidGmo,
                    ContainsGmos = containsGmo,
                    InArea = inArea,
                    LatitudeTarget = targetCoord?.Latitude,
                    LongitudeTarget = targetCoord?.Longitude,
                    PokemonLatitude = pokemonCoords?.Latitude,
                    PokemonLongitude = pokemonCoords?.Longitude,
                    PokemonEncounterId = payload.PokemonEncounterId,
                },
            };
        }

        private async Task HandleConsumables(ConsumerData data)
        {
            var now = DateTime.UtcNow.ToTotalSeconds();
            if (data.Cells.Count > 0)
            {
                var stopwatch = new Stopwatch();
                var updatedCells = new List<Cell>();
                stopwatch.Start();
                foreach (var cellId in data.Cells)
                {
                    var s2cell = new S2Cell(new S2CellId(cellId));
                    var center = s2cell.RectBound.Center;
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
                if (updatedCells.Count > 0)
                {
                    //await _cellRepository.AddOrUpdateAsync(updatedCells).ConfigureAwait(false);
                    foreach (var cell in updatedCells)
                    {
                        if (!await _cellRepository.ContainsAsync(cell))
                        {
                            await _cellRepository.AddAsync(cell).ConfigureAwait(false);
                        }
                        else
                        {
                            await _cellRepository.UpdateAsync(cell).ConfigureAwait(false);
                        }
                    }
                }

                stopwatch.Stop();
                if (updatedCells.Count > 0)
                {
                    _logger.LogInformation($"[Proto] [{data.Uuid}] S2Cell Count: {updatedCells.Count} parsed in {stopwatch.Elapsed.TotalSeconds}s");
                }
            }

            if (data.ClientWeather.Count > 0)
            {
                var stopwatch = new Stopwatch();
                var updatedWeather = new List<Weather>();
                stopwatch.Start();
                foreach (var weather in data.ClientWeather)
                {
                    var s2cell = new S2Cell(new S2CellId((ulong)weather.S2CellId));
                    var center = s2cell.RectBound.Center;
                    var alert = weather.Alerts?.FirstOrDefault();
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
                if (updatedWeather.Count > 0)
                {
                    await _weatherRepository.AddOrUpdateAsync(updatedWeather).ConfigureAwait(false);
                }

                stopwatch.Stop();
                if (updatedWeather.Count > 0)
                {
                    _logger.LogInformation($"[Proto] [{data.Uuid}] Weather Count: {updatedWeather.Count} parsed in {stopwatch.Elapsed.TotalSeconds}s");
                }
            }

            if (data.Forts.Count > 0)
            {
                var stopwatch = new Stopwatch();
                var updatedPokestops = new List<Pokestop>();
                var updatedGyms = new List<Gym>();
                stopwatch.Start();
                foreach (var item in data.Forts)
                {
                    ulong cellId = Convert.ToUInt64(item.cell);
                    var fort = (PokemonFortProto)item.data;
                    switch (fort.FortType)
                    {
                        case FortType.Gym:
                            var oldGym = await _gymRepository.GetByIdAsync(fort.FortId).ConfigureAwait(false);
                            var gym = new Gym(cellId, fort);
                            if (gym.Update(oldGym)) // TODO: Check HasChanges property
                            {
                                updatedGyms.Add(gym);
                            }
                            if (!_gymIdsPerCell.ContainsKey(cellId))
                            {
                                _gymIdsPerCell.Add(cellId, new List<string>());
                            }
                            _gymIdsPerCell[cellId].Add(fort.FortId);
                            break;
                        case FortType.Checkpoint:
                            var oldPokestop = await _pokestopRepository.GetByIdAsync(fort.FortId).ConfigureAwait(false);
                            var pokestop = new Pokestop(cellId, fort);
                            if (pokestop.Update(oldPokestop)) // TODO: Check HasChanges property
                            {
                                updatedPokestops.Add(pokestop);
                            }
                            if (!_stopIdsPerCell.ContainsKey(cellId))
                            {
                                _stopIdsPerCell.Add(cellId, new List<string>());
                            }
                            _stopIdsPerCell[cellId].Add(fort.FortId);
                            break;
                    }
                }
                if (updatedGyms.Count > 0)
                {
                    await _gymRepository.AddOrUpdateAsync(updatedGyms).ConfigureAwait(false);
                }
                if (updatedPokestops.Count > 0)
                {
                    await _pokestopRepository.AddOrUpdateAsync(updatedPokestops).ConfigureAwait(false);
                }

                stopwatch.Stop();
                if (updatedGyms.Count > 0 || updatedPokestops.Count > 0)
                {
                    _logger.LogInformation($"[Proto] [{data.Uuid}] Fort Count: {updatedGyms.Count + updatedPokestops.Count} parsed in {stopwatch.Elapsed.TotalSeconds}s");
                }
            }

            if (data.FortDetails.Count > 0)
            {
                var stopwatch = new Stopwatch();
                var updatedGyms = new List<Gym>();
                var updatedPokestops = new List<Pokestop>();
                stopwatch.Start();
                foreach (var details in data.FortDetails)
                {
                    switch (details.FortType)
                    {
                        case FortType.Gym:
                            var gym = await _gymRepository.GetByIdAsync(details.Id).ConfigureAwait(false);
                            if (gym != null)
                            {
                                gym.AddDetails(details);
                                if (gym.Update(gym))
                                {
                                    updatedGyms.Add(gym);
                                }
                            }
                            break;
                        case FortType.Checkpoint:
                            var pokestop = await _pokestopRepository.GetByIdAsync(details.Id).ConfigureAwait(false);
                            if (pokestop != null)
                            {
                                pokestop.AddDetails(details);
                                if (pokestop.Update(pokestop))
                                {
                                    updatedPokestops.Add(pokestop);
                                }
                            }
                            break;
                    }
                }
                if (updatedGyms.Count > 0)
                {
                    await _gymRepository.AddOrUpdateAsync(updatedGyms).ConfigureAwait(false);
                }
                if (updatedPokestops.Count > 0)
                {
                    await _pokestopRepository.AddOrUpdateAsync(updatedPokestops).ConfigureAwait(false);
                }

                stopwatch.Stop();
                if (updatedGyms.Count > 0 || updatedPokestops.Count > 0)
                {
                    _logger.LogInformation($"[Proto] [{data.Uuid}] FortDetails Count: {updatedGyms.Count + updatedPokestops.Count} parsed in {stopwatch.Elapsed.TotalSeconds}s");
                }
            }

            if (data.FortSearch.Count > 0)
            {
                // TODO: FortSearch, never actually used
            }

            if (data.GymInfo.Count > 0)
            {
                var stopwatch = new Stopwatch();
                var updatedGyms = new List<Gym>();
                var updatedDefenders = new List<GymDefender>();
                var updatedTrainers = new List<Trainer>();
                stopwatch.Start();
                foreach (var gymInfo in data.GymInfo)
                {
                    if (gymInfo.GymStatusAndDefenders == null)
                    {
                        _logger.LogWarning($"[ConsumerService] Invalid GymStatusAndDefenders provided, skipping...\n: {gymInfo}");
                        continue;
                    }
                    var id = gymInfo.GymStatusAndDefenders.PokemonFortProto.FortId;
                    var gymDefenders = gymInfo.GymStatusAndDefenders.GymDefender;
                    if (gymDefenders == null)
                        continue;

                    foreach (var gymDefender in gymDefenders)
                    {
                        var trainerProfile = gymDefender.TrainerPublicProfile;
                        updatedTrainers.Add(new Trainer
                        {
                            Name = trainerProfile.Name,
                            Level = (ushort)trainerProfile.Level,
                            TeamId = (ushort)trainerProfile.Team,
                            BattlesWon = (uint)(trainerProfile?.BattlesWon ?? 0),
                            KmWalked = trainerProfile?.KmWalked ?? 0,
                            PokemonCaught = (ulong)(trainerProfile?.CaughtPokemon ?? 0),
                            Experience = (ulong)(trainerProfile?.Experience ?? 0),
                            CombatRank = (ulong)(trainerProfile?.CombatRank ?? 0),
                            CombatRating = trainerProfile?.CombatRating ?? 0,
                        });

                        var defenderPokemon = gymDefender.MotivatedPokemon;
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
                            PvpCombatWon = (ushort)defenderPokemon.Pokemon.PvpCombatStats?.NumWon,
                            PvpCombatTotal = (ushort)defenderPokemon.Pokemon.PvpCombatStats?.NumTotal,
                            NpcCombatWon = (ushort)defenderPokemon.Pokemon.NpcCombatStats?.NumWon,
                            NpcCombatTotal = (ushort)defenderPokemon.Pokemon.NpcCombatStats?.NumTotal,
                        });
                    }

                    var gym = await _gymRepository.GetByIdAsync(id).ConfigureAwait(false);
                    if (gym != null)
                    {
                        gym.AddDetails(gymInfo);
                        if (gym.Update(gym)) // TODO: Check HasChanges property
                        {
                            updatedGyms.Add(gym);
                        }
                    }
                }

                if (updatedGyms.Count > 0)
                {
                    await _gymRepository.AddOrUpdateAsync(updatedGyms).ConfigureAwait(false);
                }
                if (updatedTrainers.Count > 0)
                {
                    await _trainerRepository.AddOrUpdateAsync(updatedTrainers).ConfigureAwait(false);
                }
                if (updatedDefenders.Count > 0)
                {
                    await _gymDefenderRepository.AddOrUpdateAsync(updatedDefenders).ConfigureAwait(false);
                }

                stopwatch.Stop();
                if (updatedGyms.Count > 0)
                {
                    _logger.LogInformation($"[Proto] [{data.Uuid}] GymGetInfo Count: {updatedGyms.Count} parsed in {stopwatch.Elapsed.TotalSeconds}s");
                }
                if (updatedTrainers.Count > 0)
                {
                    _logger.LogInformation($"[Proto] [{data.Uuid}] Gym Trainer Count: {updatedTrainers.Count} parsed in {stopwatch.Elapsed.TotalSeconds}s");
                }
                if (updatedDefenders.Count > 0)
                {
                    _logger.LogInformation($"[Proto] [{data.Uuid}] Gym Defender Count: {updatedDefenders.Count} parsed in {stopwatch.Elapsed.TotalSeconds}s");
                }
            }

            if (data.WildPokemon.Count > 0 || data.NearbyPokemon.Count > 0)
            {
                var stopwatch = new Stopwatch();
                var updatedPokemon = new List<Pokemon>();
                var updatedSpawnpoints = new List<Spawnpoint>();
                stopwatch.Start();
                if (data.WildPokemon.Count > 0)
                {
                    foreach (var item in data.WildPokemon)
                    {
                        var cell = (ulong)item.cell;
                        var wildPokemon = (WildPokemonProto)item.data;
                        var timestampMs = (ulong)item.timestamp_ms;
                        var username = item.username;
                        //var id = wildPokemon.EncounterId;
                        var pokemon = new Pokemon(wildPokemon, cell, timestampMs, username, false); // TODO: IsEvent
                        var oldPokemon = await _pokemonRepository.GetByIdAsync(pokemon.Id).ConfigureAwait(false);
                        if (pokemon.Update(oldPokemon, true)) // TODO: Check HasChanges property
                        {
                            updatedPokemon.Add(pokemon);
                        }
                        if (pokemon.CellId == 0)
                        {
                            Console.WriteLine($"Pokemon: {pokemon}");
                        }
                        var spawnpoint = pokemon.HandleSpawnpoint(wildPokemon.TimeTillHiddenMs, timestampMs)
                                                .ConfigureAwait(false)
                                                .GetAwaiter()
                                                .GetResult();
                        updatedSpawnpoints.Add(spawnpoint);
                    }
                }
                if (data.NearbyPokemon.Count > 0)
                {
                    foreach (var item in data.NearbyPokemon)
                    {
                        var cell = (ulong)item.cell;
                        // data.timestamp_ms
                        var nearbyPokemon = (NearbyPokemonProto)item.data;
                        var username = item.username;
                        var pokemon = new Pokemon(nearbyPokemon, cell, username, false); // TODO: IsEvent
                        if (pokemon.Latitude == 0 && string.IsNullOrEmpty(pokemon.PokestopId))
                        {
                            // Skip nearby pokemon without pokestop id set and no coordinate
                            continue;
                        }
                        var pokestop = await _pokestopRepository.GetByIdAsync(pokemon.PokestopId).ConfigureAwait(false);
                        if (pokestop == null)
                        {
                            // Unknown stop, skip pokemon
                            continue;
                        }
                        pokemon.Latitude = pokestop.Latitude;
                        pokemon.Longitude = pokestop.Longitude;
                        var oldPokemon = _pokemonRepository.GetByIdAsync(pokemon.Id)
                                                          .ConfigureAwait(false)
                                                          .GetAwaiter()
                                                          .GetResult();
                        if (pokemon.Update(oldPokemon)) // TODO: Check HasChanges property
                        {
                            updatedPokemon.Add(pokemon);
                        }
                        if (pokemon.CellId == 0)
                        {
                            Console.WriteLine($"Pokemon: {pokemon}");
                        }
                    }
                }
                if (updatedSpawnpoints.Count > 0)
                {
                    await _spawnpointRepository.AddOrUpdateAsync(updatedSpawnpoints).ConfigureAwait(false);
                }
                if (updatedPokemon.Count > 0)
                {
                    await _pokemonRepository.AddOrUpdateAsync(updatedPokemon).ConfigureAwait(false);
                }

                stopwatch.Stop();
                if (updatedSpawnpoints.Count > 0)
                {
                    _logger.LogInformation($"[Proto] [{data.Uuid}] Spawnpoint Count: {updatedSpawnpoints.Count} parsed in {stopwatch.Elapsed.TotalSeconds}s");
                }
                if (updatedPokemon.Count > 0)
                {
                    _logger.LogInformation($"[Proto] [{data.Uuid}] WildPokemon Count: {data.WildPokemon.Count} NearbyPokemon Count: {data.NearbyPokemon.Count} parsed in {stopwatch.Elapsed.TotalSeconds}s");
                }
            }

            if (data.Encounters.Count > 0)
            {
                var stopwatch = new Stopwatch();
                var updatedPokemon = new List<Pokemon>();
                var updatedSpawnpoints = new List<Spawnpoint>();
                stopwatch.Start();
                foreach (var item in data.Encounters)
                {
                    var encounter = (EncounterOutProto)item.encounter;
                    var username = item.username;
                    Pokemon pokemon;
                    try
                    {
                        pokemon = await _pokemonRepository.GetByIdAsync(encounter.Pokemon.EncounterId.ToString()).ConfigureAwait(false); // TODO: is_event
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error: {ex}");
                        pokemon = null;
                    }
                    if (pokemon != null)
                    {
                        await pokemon.AddEncounter(encounter, username).ConfigureAwait(false);
                        if (pokemon.Update(pokemon, true))
                        {
                            updatedPokemon.Add(pokemon);
                        }
                        if (pokemon.CellId == 0)
                        {
                            Console.WriteLine($"Pokemon: {pokemon}");
                        }
                    }
                    else
                    {
                        var centerCoord = new Coordinate(encounter.Pokemon.Latitude, encounter.Pokemon.Longitude);
                        var cellId = S2CellId.FromLatLng(S2LatLng.FromDegrees(centerCoord.Latitude, centerCoord.Longitude));
                        var timestampMs = DateTime.UtcNow.ToTotalSeconds() * 1000;
                        var newPokemon = new Pokemon(encounter.Pokemon, cellId.Id, timestampMs, username, false); // TODO: IsEvent
                        await newPokemon.AddEncounter(encounter, username).ConfigureAwait(false);
                        if (newPokemon.Update(null, true))
                        {
                            updatedPokemon.Add(newPokemon);
                        }
                        if (newPokemon.CellId == 0)
                        {
                            Console.WriteLine($"Pokemon: {newPokemon}");
                        }
                        var spawnpoint = await newPokemon.HandleSpawnpoint(encounter.Pokemon.TimeTillHiddenMs, timestampMs).ConfigureAwait(false);
                        updatedSpawnpoints.Add(spawnpoint);
                    }
                }

                if (updatedSpawnpoints.Count > 0)
                {
                    await _spawnpointRepository.AddOrUpdateAsync(updatedSpawnpoints).ConfigureAwait(false);
                }
                if (updatedPokemon.Count > 0)
                {
                    await _pokemonRepository.AddOrUpdateAsync(updatedPokemon).ConfigureAwait(false);
                }

                stopwatch.Stop();
                if (updatedSpawnpoints.Count > 0)
                {
                    _logger.LogInformation($"[Proto] [{data.Uuid}] Spawnpoint Count: {updatedSpawnpoints.Count} parsed in {stopwatch.Elapsed.TotalSeconds}s");
                }
                if (updatedPokemon.Count > 0)
                {
                    _logger.LogInformation($"[Proto] [{data.Uuid}] Encounter Count: {updatedPokemon.Count} parsed in {stopwatch.Elapsed.TotalSeconds}s");
                }
            }

            if (data.Quests.Count > 0)
            {
                var stopwatch = new Stopwatch();
                var updatedQuests = new List<Pokestop>();
                stopwatch.Start();
                foreach (var quest in data.Quests)
                {
                    // Get existing pokestop, and add quest to it
                    var pokestop = await _pokestopRepository.GetByIdAsync(quest.FortId).ConfigureAwait(false);
                    // Skip quests we don't have stops for yet
                    if (pokestop == null)
                        continue;
                    /*
                    if (await pokestop.TriggerWebhook(true))
                    {
                        _logger.LogDebug($"[Quest] Found a quest belonging to a new stop, skipping..."); // :face_with_raised_eyebrow:
                        continue;
                    }
                    */
                    pokestop.AddQuest(quest);
                    if (pokestop.Update(pokestop, true)) // TODO: Check HasChanges property
                    {
                        updatedQuests.Add(pokestop);
                    }
                }
                if (updatedQuests.Count > 0)
                {
                    await _pokestopRepository.AddOrUpdateAsync(updatedQuests, false).ConfigureAwait(false);
                }

                stopwatch.Stop();
                if (updatedQuests.Count > 0)
                {
                    _logger.LogInformation($"[Proto] [{data.Uuid}] Quest Count: {updatedQuests.Count} parsed in {stopwatch.Elapsed.TotalSeconds}s");
                }
            }

            if (data.PlayerData.Count > 0)
            {
                var stopwatch = new Stopwatch();
                var updatedAccounts = new List<Account>();
                stopwatch.Start();
                foreach (var item in data.PlayerData)
                {
                    var username = item.username;
                    var playerData = (GetPlayerOutProto)item.gpr;
                    // Get account
                    var account = await _accountRepository.GetByIdAsync(username).ConfigureAwait(false);
                    // Skip account if we failed to get it
                    if (account == null)
                        continue;

                    account.CreationTimestamp = (ulong)playerData.Player.CreationTimeMs / 1000;
                    account.Warn = playerData.Warn;
                    var warnExpireTimestamp = (ulong)playerData.WarnExpireMs / 1000;
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
                if (updatedAccounts.Count > 0)
                {
                    // TODO: Ignore gpr warn/ban columns if overwriting
                    await _accountRepository.AddOrUpdateAsync(updatedAccounts).ConfigureAwait(false);
                }

                stopwatch.Stop();
                if (updatedAccounts.Count > 0)
                {
                    _logger.LogInformation($"[Proto] [{data.Uuid}] Quest Count: {updatedAccounts.Count} parsed in {stopwatch.Elapsed.TotalSeconds}s");
                }
            }

            // TODO: Enable Clearing
            await Task.CompletedTask;
        }

        /*
        private async Task<(bool, ulong, Spawnpoint)> HandleSpawnpoint(ulong? spawnId, double latitude, double longitude, int timeTillHiddenMs, ulong timestampMs)
        {
            var now = DateTime.UtcNow.ToTotalSeconds();
            bool verifiedTimer;
            ulong expireTimestamp = 0;
            if (timeTillHiddenMs <= 90000 && timeTillHiddenMs > 0)
            {
                expireTimestamp = Convert.ToUInt64(timestampMs + Convert.ToDouble(timeTillHiddenMs)) / 1000;
                verifiedTimer = true;
                var unixDate = timestampMs.FromMilliseconds();
                var secondOfHour = unixDate.Second + unixDate.Minute * 60;
                var spawnpoint = new Spawnpoint
                {
                    Id = spawnId ?? 0,
                    Latitude = latitude,
                    Longitude = longitude,
                    Updated = now,
                    DespawnSecond = (ushort)secondOfHour,
                };
                return (verifiedTimer, expireTimestamp, spawnpoint);
            }
            else
            {
                verifiedTimer = false;
            }

            if (!verifiedTimer && spawnId != null)
            {
                Spawnpoint spawnpoint = null;
                try
                {
                    if (spawnId != null)
                    {
                        spawnpoint = await _spawnpointRepository.GetByIdAsync(spawnId ?? 0);
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
                    expireTimestamp = now + despawnOffset;
                    verifiedTimer = true;
                }
                else
                {
                    spawnpoint = new Spawnpoint
                    {
                        Id = spawnId ?? 0,
                        Latitude = latitude,
                        Longitude = longitude,
                        Updated = DateTime.Now.ToTotalSeconds(),
                        DespawnSecond = null,
                    };
                }
                return (verifiedTimer, expireTimestamp, spawnpoint);
            }

            if (expireTimestamp == 0)
            {
                //Console.WriteLine($"[Pokemon] ExpireTimestamp == 0");
                expireTimestamp = DateTime.UtcNow.ToTotalSeconds();
                verifiedTimer = false;
            }
            return (verifiedTimer, expireTimestamp, null);
        }
        */

        #endregion

        private Task PublishData<T>(string channel, T data)
        {
            try
            {
                if (data == null)
                {
                    return Task.CompletedTask;
                }
                _subscriber.PublishAsync(channel, data.ToJson(), CommandFlags.FireAndForget);
            }
            catch (Exception ex)
            {
                ConsoleExt.WriteError(ex);
            }
            return Task.CompletedTask;
        }
    }
}