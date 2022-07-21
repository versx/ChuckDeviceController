namespace ChuckDeviceController.Services
{
    using System;
    using System.Diagnostics;

    using Google.Common.Geometry;
    using POGOProtos.Rpc;

    using ChuckDeviceController.Cache;
    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Extensions;
    using ChuckDeviceController.Geometry.Extensions;
    using ChuckDeviceController.Geometry.Models;
    using ChuckDeviceController.HostedServices;
    using ChuckDeviceController.Net.Models.Requests;
    using ChuckDeviceController.Protos;

    public class ProtoPayloadItem
    {
        public ProtoPayload Payload { get; set; }

        public Device Device { get; set; }
    }

    public class ProtoProcessorService : BackgroundService, IProtoProcessorService
    {
        #region Variables

        private readonly ILogger<IProtoProcessorService> _logger;
        private readonly IBackgroundTaskQueue _taskQueue;
        private readonly IDataProcessorService _dataProcessor;

        private readonly Dictionary<ulong, int> _emptyCells;
        private static readonly TimedMap<bool> _arQuestActualMap = new();
        private static readonly TimedMap<bool> _arQuestTargetMap = new();

        #endregion

        #region Properties

        public bool ProcessMapPokemon { get; set; }

        #endregion

        #region Constructor

        public ProtoProcessorService(
            ILogger<IProtoProcessorService> logger,
            IBackgroundTaskQueue taskQueue,
            IDataProcessorService dataProcessor)
        {
            _logger = logger;
            _taskQueue = (DefaultBackgroundTaskQueue)taskQueue;
            _dataProcessor = dataProcessor;
            _emptyCells = new Dictionary<ulong, int>();

            ProcessMapPokemon = true; // TODO: Make `ProcessMapPokemon` configurable
        }

        #endregion

        #region Background Service

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                $"{nameof(IProtoProcessorService)} is stopping.");

            await base.StopAsync(stoppingToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                $"{nameof(IProtoProcessorService)} is now running in the background.");

            await BackgroundProcessing(stoppingToken);
        }

        private async Task BackgroundProcessing(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    //var workItem = await _taskQueue.DequeueAsync(stoppingToken);
                    //await workItem(stoppingToken);
                    var workItems = await _taskQueue.DequeueMultipleAsync(Strings.MaximumQueueBatchSize, stoppingToken);
                    //var tasks = workItems.Select(item => Task.Factory.StartNew(async () => await item(stoppingToken)));
                    //Task.WaitAll(tasks.ToArray(), stoppingToken);

                    await Task.Run(() =>
                    {
                        foreach (var workItem in workItems)
                        {
                            Task.Factory.StartNew(async () => await workItem(stoppingToken));
                        }
                    }, stoppingToken);

                    /*
                    foreach (var workItem in workItems)
                    {
                        await workItem(stoppingToken);
                    }
                    */
                }
                catch (OperationCanceledException)
                {
                    // Prevent throwing if stoppingToken was signaled
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred executing task work item.");
                }
                //await Task.Delay(10, stoppingToken);
                Thread.Sleep(5);
            }

            _logger.LogError("Exited background processing...");
        }

        public async Task EnqueueAsync(ProtoPayloadItem payload)
        {
            await _taskQueue.EnqueueAsync(async token =>
                await ProcessWorkItemAsync(payload, token));
        }

        private async Task<CancellationToken> ProcessWorkItemAsync(
            ProtoPayloadItem payload,
            CancellationToken stoppingToken)
        {
            if (payload?.Payload == null || payload?.Device == null)
                return stoppingToken;

            CheckQueueLength();

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var uuid = payload.Payload.Uuid;
            var username = payload.Payload.Username;
            var level = payload.Payload.Level;
            var xp = payload.Payload.TrainerXp ?? 0;
            var timestamp = payload.Payload.Timestamp ?? DateTime.UtcNow.ToTotalSeconds();
            var hasArQuestReqGlobal = payload.Payload.HaveAr;

            var isMadData = false;
            var isEmptyGmo = true;
            var isInvalidGmo = true;
            var containsGmo = false;

            Coordinate? targetCoord = null;
            var inArea = false;
            if (payload.Payload.LatitudeTarget != 0 && payload.Payload.LongitudeTarget != 0)
            {
                targetCoord = new Coordinate(payload.Payload.LatitudeTarget, payload.Payload.LongitudeTarget);
            }
            var targetKnown = false;
            S2CellId targetCellId = default;
            if (targetCoord != null)
            {
                // Check target is within cell id instead of checking geofences
                targetKnown = true;
                targetCellId = targetCoord.S2CellIdFromCoordinate();
                //_logger.LogDebug($"[{uuid}] Data received within target area {targetCoord} and target distance {payload.TargetMaxDistance}");
            }
            //_logger.LogWarning($"[{device.Uuid}] InArea={inArea}");

            var processedProtos = new List<dynamic>();

            foreach (var rawData in payload.Payload.Contents)
            {
                if (string.IsNullOrEmpty(rawData.Data))
                {
                    _logger.LogWarning($"[{uuid}] Unhandled proto {rawData.Method}: Proto data is null '{rawData.Data}'");
                    continue;
                }
                var data = rawData.Data;
                var method = (Method)rawData.Method;
                var hasArQuestReq = rawData.HaveAr;
                switch (method)
                {
                    /*
                    case Method.GetPlayer:
                        try
                        {
                            var gpr = GetPlayerOutProto.Parser.ParseFrom(Convert.FromBase64String(data));
                            if (gpr?.Success ?? false)
                            {
                                processedProtos.Add(new
                                {
                                    type = ProtoDataType.PlayerData,
                                    gpr,
                                    username,
                                });
                            }
                            else
                            {
                                _logger.LogError($"[{uuid}] Malformed GetPlayerOutProto");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"[{uuid}] Unable to decode GetPlayerOutProto: {ex}");
                        }
                        break;
                    */
                    case Method.GetHoloholoInventory:
                        try
                        {
                            var ghi = GetHoloholoInventoryOutProto.Parser.ParseFrom(Convert.FromBase64String(data));
                            if (!(ghi?.Success ?? false))
                            {
                                _logger.LogError($"[{uuid}] Malformed GetHoloholoInventoryOutProto");
                                continue;
                            }

                            var inventoryItems = ghi.InventoryDelta?.InventoryItem;
                            if (inventoryItems == null)
                                continue;

                            foreach (var item in inventoryItems)
                            {
                                var itemData = item.InventoryItemData;
                                if (itemData == null)
                                    continue;

                                if ((itemData?.PlayerStats?.Experience ?? 0) > 0)
                                {
                                    xp = Convert.ToUInt32(itemData?.PlayerStats?.Experience ?? 0);
                                }

                                var quests = itemData?.Quests?.Quest;
                                if (uuid != null && (quests?.Count ?? 0) > 0)
                                {
                                    foreach (var quest in quests)
                                    {
                                        if (quest.QuestContext == QuestProto.Types.Context.ChallengeQuest &&
                                            quest.QuestType == QuestType.QuestGeotargetedArScan)
                                        {
                                            _arQuestActualMap.Set(uuid, true, timestamp);
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"[{uuid}] Unable to decode GetHoloholoInventoryOutProto: {ex}");
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
                                    // Check for AR quest or if AR quest is required
                                    var hasAr = hasArQuestReqGlobal
                                        ?? hasArQuestReq
                                        ?? GetArQuestMode(uuid, timestamp);
                                    var title = fsr.ChallengeQuest.QuestDisplay.Title;
                                    var quest = fsr.ChallengeQuest.Quest;
                                    _logger.LogDebug($"[{uuid}] Has AR: {hasAr}");

                                    if (quest.QuestType == QuestType.QuestGeotargetedArScan && uuid != null)
                                    {
                                        _arQuestActualMap.Set(uuid, true, timestamp);
                                    }
                                    processedProtos.Add(new
                                    {
                                        type = ProtoDataType.Quest,
                                        title,
                                        quest,
                                        hasAr,
                                    });
                                    //quests++;
                                }
                            }
                            else
                            {
                                _logger.LogError($"[{uuid}] Malformed FortSearchOutProto");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"[{uuid}] Unable to decode FortSearchOutProto: {ex}");
                        }
                        break;
                    case Method.Encounter:
                        isEmptyGmo = false;
                        isInvalidGmo = false;
                        try
                        {
                            if (level >= 30 || isMadData)
                            {
                                var er = EncounterOutProto.Parser.ParseFrom(Convert.FromBase64String(data));
                                if (er?.Status == EncounterOutProto.Types.Status.EncounterSuccess)
                                {
                                    processedProtos.Add(new
                                    {
                                        type = ProtoDataType.Encounter,
                                        data = er,
                                        username,
                                        isEvent = false,
                                    });
                                }
                                else if (er == null)
                                {
                                    _logger.LogError($"[{uuid}] Malformed EncounterOutProto");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"[{uuid}] Unable to decode EncounterOutProto: {ex}");
                        }
                        break;
                    case Method.DiskEncounter:
                        if (ProcessMapPokemon && (level >= 30 || isMadData))
                        {
                            try
                            {
                                var der = DiskEncounterOutProto.Parser.ParseFrom(Convert.FromBase64String(data));
                                if (der?.Result == DiskEncounterOutProto.Types.Result.Success)
                                {
                                    processedProtos.Add(new
                                    {
                                        type = ProtoDataType.DiskEncounter,
                                        data = der,
                                        username,
                                        isEvent = false,
                                    });
                                }
                                else
                                {
                                    _logger.LogError($"[{uuid}] Malformed DiskEncounterOutProto");
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError($"[{uuid}] Unable to decode DiskEncounterOutProto: {ex}");
                            }
                        }
                        break;
                    case Method.FortDetails:
                        try
                        {
                            var fdr = FortDetailsOutProto.Parser.ParseFrom(Convert.FromBase64String(data));
                            if (fdr != null)
                            {
                                processedProtos.Add(new
                                {
                                    type = ProtoDataType.FortDetails,
                                    data = fdr,
                                });
                            }
                            else
                            {
                                _logger.LogError($"[{uuid}] Malformed FortDetailsOutProto");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"[{uuid}] Unable to decode FortDetailsOutProto: {ex}");
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
                                var gmoMapCells = gmo.MapCell;
                                var newWildPokemon = new List<dynamic>();
                                var newNearbyPokemon = new List<dynamic>();
                                var newMapPokemon = new List<dynamic>();
                                var newClientWeather = new List<dynamic>();
                                var newForts = new List<dynamic>();
                                var newCells = new List<dynamic>();

                                if (gmoMapCells.Count == 0)
                                {
                                    _logger.LogDebug($"[{uuid}] Map cells are empty");
                                    continue;
                                }

                                // Check if we're within the same cell, if so then we are within the target distance
                                if (!inArea && targetKnown && gmoMapCells.Select(x => x.S2CellId).Contains(targetCellId.Id))
                                {
                                    inArea = true;
                                }

                                foreach (var mapCell in gmoMapCells)
                                {
                                    var timestampMs = Convert.ToUInt64(mapCell.AsOfTimeMs);
                                    foreach (var wild in mapCell.WildPokemon)
                                    {
                                        newWildPokemon.Add(new
                                        {
                                            type = ProtoDataType.WildPokemon,
                                            cell = mapCell.S2CellId,
                                            data = wild,
                                            timestampMs,
                                            username,
                                            isEvent = false, // TODO: IsEvent
                                        });
                                    }

                                    foreach (var nearby in mapCell.NearbyPokemon)
                                    {
                                        newNearbyPokemon.Add(new
                                        {
                                            type = ProtoDataType.NearbyPokemon,
                                            cell = mapCell.S2CellId,
                                            data = nearby,
                                            username,
                                            isEvent = false,
                                        });
                                    }

                                    foreach (var fort in mapCell.Fort)
                                    {
                                        newForts.Add(new
                                        {
                                            type = ProtoDataType.Fort,
                                            cell = mapCell.S2CellId,
                                            data = fort,
                                            username,
                                        });
                                        if (ProcessMapPokemon && fort.ActivePokemon != null)
                                        {
                                            newMapPokemon.Add(new
                                            {
                                                type = ProtoDataType.MapPokemon,
                                                cell = mapCell.S2CellId,
                                                data = fort.ActivePokemon,
                                                username,
                                                isEvent = false,
                                            });
                                        }
                                    }

                                    newCells.Add(new
                                    {
                                        type = ProtoDataType.Cell,
                                        cell = mapCell.S2CellId,
                                    });
                                }

                                foreach (var weatherCell in gmo.ClientWeather)
                                {
                                    newClientWeather.Add(new
                                    {
                                        type = ProtoDataType.ClientWeather,
                                        cell = weatherCell.S2CellId,
                                        data = weatherCell,
                                    });
                                }

                                if (newWildPokemon.Count == 0 && newNearbyPokemon.Count == 0 && newForts.Count == 0)
                                {
                                    foreach (var cell in newCells)
                                    {
                                        var cellId = cell.cell;
                                        lock (_emptyCells)
                                        {
                                            if (!_emptyCells.ContainsKey(cellId))
                                            {
                                                _emptyCells.Add(cellId, 1);
                                            }
                                            else
                                            {
                                                _emptyCells[cellId]++;
                                            }
                                        }

                                        var count = _emptyCells[cellId];
                                        if (count == 3)
                                        {
                                            _logger.LogWarning($"[{uuid}] Cell {cellId} was empty 3 times in a row. Assuming empty.");
                                            processedProtos.Add(cell);
                                        }
                                    }

                                    isEmptyGmo = true;
                                    _logger.LogDebug($"[{uuid}] GMO is empty.");
                                }
                                else
                                {
                                    foreach (var cell in newCells)
                                    {
                                        lock (_emptyCells)
                                        {
                                            var cellId = cell.cell;
                                            if (_emptyCells.ContainsKey(cellId))
                                            {
                                                _emptyCells[cellId] = 0;
                                            }
                                            else
                                            {
                                                _emptyCells.Add(cellId, 0);
                                            }
                                        }
                                    }

                                    isEmptyGmo = false;
                                }

                                processedProtos.AddRange(newCells);
                                processedProtos.AddRange(newClientWeather);
                                processedProtos.AddRange(newForts);
                                processedProtos.AddRange(newWildPokemon);
                                processedProtos.AddRange(newNearbyPokemon);
                                processedProtos.AddRange(newMapPokemon);
                            }
                            else
                            {
                                _logger.LogError($"[{uuid}] Malformed GetMapObjectsOutProto");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"[{uuid}] Unable to decode GetMapObjectsOutProto: {ex}");
                        }
                        break;
                    case Method.GymGetInfo:
                        try
                        {
                            var ggi = GymGetInfoOutProto.Parser.ParseFrom(Convert.FromBase64String(data));
                            if (ggi != null)
                            {
                                processedProtos.Add(new
                                {
                                    type = ProtoDataType.GymInfo,
                                    data = ggi,
                                });

                                if (ggi.GymStatusAndDefenders == null)
                                {
                                    _logger.LogWarning($"Invalid GymStatusAndDefenders provided, skipping...\n: {ggi}");
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
                                        processedProtos.Add(new
                                        {
                                            type = ProtoDataType.Trainer,
                                            data = gymDefender.TrainerPublicProfile,
                                        });
                                    }
                                    if (gymDefender.MotivatedPokemon != null)
                                    {
                                        processedProtos.Add(new
                                        {
                                            type = ProtoDataType.GymDefender,
                                            fort = fortId,
                                            data = gymDefender,
                                        });
                                    }
                                }
                            }
                            else
                            {
                                _logger.LogError($"[{uuid}] Malformed GymGetInfoOutProto");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"[{uuid}] Unable to decode GymGetInfoOutProto: {ex}");
                        }
                        break;
                    case Method.Unset:
                    default:
                        _logger.LogDebug($"[{uuid}] Invalid method or data provided. {method}:{data}");
                        break;
                }
            }

            if (!string.IsNullOrEmpty(username) && xp > 0 && level > 0)
            {
                var playerInfo = new
                {
                    username,
                    xp,
                    level,
                };
                await GrpcClientService.SendRpcPayloadAsync(playerInfo, PayloadType.PlayerInfo, username);
            }

            stopwatch.Stop();

            // Insert/upsert wildPokemon, nearbyPokemon, mapPokemon, forts, cells, clientWeather, etc into database
            if (processedProtos.Count > 0)
            {
                var totalSeconds = Math.Round(stopwatch.Elapsed.TotalSeconds, 4);
                // _logger.LogInformation($"[{uuid}] {processedProtos.Count:N0} protos parsed in {totalSeconds}s");

                await _dataProcessor.ConsumeDataAsync(processedProtos);
            }

            return stoppingToken;
        }

        public static void SetArQuestTarget(string uuid, ulong timestamp, bool isAr)
        {
            _arQuestTargetMap.Set(uuid, isAr, timestamp);
            if (isAr)
            {
                // AR mode is sent to client -> client will clear ar quest
                _arQuestActualMap.Set(uuid, false, timestamp);
            }
        }

        private static bool GetArQuestMode(string uuid, ulong timestamp)
        {
            if (string.IsNullOrEmpty(uuid))
            {
                return true;
            }
            var targetMode = _arQuestTargetMap.Get(uuid, timestamp);
            var actualMode = _arQuestActualMap.Get(uuid, timestamp);
            if (!targetMode)
            {
                return true;
            }
            else if (targetMode)
            {
                return false;
            }
            return actualMode;
        }

        #endregion

        private void CheckQueueLength()
        {
            if (_taskQueue.Count == Strings.MaximumQueueCapacity)
            {
                _logger.LogWarning($"Proto processing queue is at maximum capacity! {_taskQueue.Count:N0}/{Strings.MaximumQueueCapacity:N0}");
            }
            else if (_taskQueue.Count > Strings.MaximumQueueSizeWarning)
            {
                _logger.LogWarning($"Proto processing queue is {_taskQueue.Count:N0} items long.");
            }
        }
    }
}