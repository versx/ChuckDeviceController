namespace ChuckProtoParser.Controllers
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

    using Chuck.Common;
    using Chuck.Common.Net.Models.Requests;
    using Chuck.Common.Net.Models.Responses;
    using Chuck.Data.Contexts;
    using Chuck.Data.Entities;
    using Chuck.Data.Repositories;
    using Chuck.Extensions;
    using Chuck.Geometry.Geofence.Models;

    [ApiController]
    public class ProtoController : ControllerBase
    {
        #region Variables

        private readonly IConnectionMultiplexer _redis;
        private readonly IDatabaseAsync _redisDatabase;
        private readonly ILogger<ProtoController> _logger;

        private readonly AccountRepository _accountRepository;
        private readonly DeviceRepository _deviceRepository;

        private readonly Dictionary<ulong, int> _emptyCells;
        private readonly Dictionary<string, ushort> _levelCache;

        //private readonly Dictionary<ulong, List<string>> _gymIdsPerCell;
        //private readonly Dictionary<ulong, List<string>> _stopIdsPerCell;

        #endregion

        #region Constructor

        public ProtoController(IDbContextFactory<DeviceControllerContext> dbFactory, IConnectionMultiplexer connectionMultiplexer, ILogger<ProtoController> logger)
        {
            _redis = connectionMultiplexer;
            _redisDatabase = _redis.GetDatabase(Startup.Config.Redis.DatabaseNum);
            _logger = logger;

            _accountRepository = new AccountRepository(dbFactory.CreateDbContext());
            _deviceRepository = new DeviceRepository(dbFactory.CreateDbContext());
            _levelCache = new Dictionary<string, ushort>();
            _emptyCells = new Dictionary<ulong, int>();
            //_gymIdsPerCell = new Dictionary<ulong, List<string>>();
            //_stopIdsPerCell = new Dictionary<ulong, List<string>>();
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

            var stopwatch = new Stopwatch();
            stopwatch.Start();
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
            var wildPokemon = 0;
            var nearbyPokemon = 0;
            var clientWeather = 0;
            var forts = 0;
            var fortDetails = new List<FortDetailsOutProto>();
            var quests = 0;
            var fortSearch = 0;
            var encounters = 0;
            var cells = new List<ulong>();
            var inventory = new List<InventoryDeltaProto>();
            var playerData = 0;
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
                                await PushData(RedisChannels.ProtoAccount, new
                                {
                                    gpr,
                                    username = payload.Username,
                                });
                                playerData++;
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
                                    // TODO: Publish with redis
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
                                    await PushData(RedisChannels.ProtoQuest, new
                                    {
                                        raw = data,
                                    });
                                    quests++;
                                }
                                //fortSearch.Add(fsr);
                                fortSearch++;
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
                                    await PushData(RedisChannels.ProtoEncounter, new
                                    {
                                        data = er,
                                        username = payload.Username,
                                    });
                                    encounters++;
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
                                //fortDetails++;
                                // TODO: Publish with redis
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
                                    cells.Add(mapCell.S2CellId);
                                    await PushData(RedisChannels.ProtoCell, mapCell.S2CellId);

                                    var tsMs = mapCell.AsOfTimeMs;
                                    foreach (var wild in mapCell.WildPokemon)
                                    {
                                        await PushData(RedisChannels.ProtoWildPokemon, new
                                        {
                                            cell = mapCell.S2CellId,
                                            data = wild,
                                            timestamp_ms = tsMs,
                                            username = payload.Username,
                                        });
                                        wildPokemon++;
                                    }
                                    foreach (var nearby in mapCell.NearbyPokemon)
                                    {
                                        await PushData(RedisChannels.ProtoNearbyPokemon, new
                                        {
                                            cell = mapCell.S2CellId,
                                            data = nearby,
                                            timestamp_ms = tsMs,
                                            username = payload.Username,
                                        });
                                        nearbyPokemon++;
                                    }
                                    foreach (var fort in mapCell.Fort)
                                    {
                                        await PushData(RedisChannels.ProtoFort, new
                                        {
                                            cell = mapCell.S2CellId,
                                            data = fort,
                                        });
                                        forts++;
                                    }
                                }
                                foreach (var weather in gmo.ClientWeather)
                                {
                                    await PushData(RedisChannels.ProtoWeather, new Weather(weather));
                                    clientWeather++;
                                }
                                if (wildPokemon == 0 && nearbyPokemon == 0 && forts == 0 && quests == 0)
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
                                            await PushData(RedisChannels.ProtoCell, cellId);
                                        }
                                    }
                                    _logger.LogDebug($"[Proto] [{payload.Uuid}] GMO is empty");
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
                                if (ggi.GymStatusAndDefenders == null)
                                {
                                    ConsoleExt.WriteWarn($"[DataConsumer] Invalid GymStatusAndDefenders provided, skipping...\n: {ggi}");
                                    continue;
                                }
                                var fortId = ggi.GymStatusAndDefenders.PokemonFortProto.FortId;
                                var gymDefenders = ggi.GymStatusAndDefenders.GymDefender;
                                if (gymDefenders == null)
                                    continue;

                                foreach (var gymDefender in gymDefenders)
                                {
                                    if (gymDefender.TrainerPublicProfile != null)
                                    {
                                        await PushData(RedisChannels.ProtoGymTrainer, new Trainer(gymDefender));
                                    }
                                    if (gymDefender.MotivatedPokemon != null)
                                    {
                                        await PushData(RedisChannels.ProtoGymDefender, new GymDefender(fortId, gymDefender));
                                    }
                                }
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

            stopwatch.Stop();

            var response = new ProtoResponse
            {
                Status = "ok",
                Data = new ProtoDataDetails
                {
                    Nearby = nearbyPokemon,
                    Wild = wildPokemon,
                    Forts = forts,
                    Quests = quests,
                    FortSearch = fortSearch,
                    Encounters = encounters,
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
            _logger.LogInformation($"[{payload.Uuid}] {response.ToJson()}");
            return response;
        }

        #endregion

        private Task PushData<T>(string channel, T data)
        {
            try
            {
                if (data == null)
                {
                    return Task.CompletedTask;
                }
                //_subscriber.PublishAsync(channel, data.ToJson(), CommandFlags.FireAndForget);
                _redisDatabase.ListRightPushAsync(Startup.Config.Redis.QueueName, new { channel, data, }.ToJson());
            }
            catch (Exception ex)
            {
                ConsoleExt.WriteError(ex);
            }
            return Task.CompletedTask;
        }
    }
}