namespace ChuckDeviceController.Services
{
    using System.Collections.Concurrent;
    using System.Diagnostics;

    using Microsoft.Extensions.Options;
    using POGOProtos.Rpc;

    using ChuckDeviceController.Collections.Cache;
    using ChuckDeviceController.Collections.Queues;
    using ChuckDeviceController.Configuration;
    using ChuckDeviceController.Extensions;
    using ChuckDeviceController.Extensions.Json;
    using ChuckDeviceController.Net.Models.Requests;
    using ChuckDeviceController.Protos;
    using ChuckDeviceController.Services.Rpc;

    public class ProtoProcessorService : BackgroundService, IProtoProcessorService
    {
        #region Constants

        private const ushort DefaultMaxEmptyCellsCount = 3;
        private const uint DefaultArCacheLimit = 1000;

        #endregion

        #region Variables

        private readonly ILogger<IProtoProcessorService> _logger;
        private readonly IAsyncQueue<ProtoPayloadQueueItem> _protoQueue;
        private readonly IAsyncQueue<DataQueueItem> _dataQueue;
        private readonly IGrpcClient<Payload.PayloadClient, PayloadRequest, PayloadResponse> _grpcProtoClient;
        private readonly IGrpcClient<Leveling.LevelingClient, TrainerInfoRequest, TrainerInfoResponse> _grpcLevelingClient;

        private static readonly ConcurrentDictionary<ulong, int> _emptyCells = new();
        private static readonly ConcurrentDictionary<string, bool> _canStoreData = new();
        private static readonly TimedMapCollection<string, bool> _arQuestActualMap = new(DefaultArCacheLimit);
        private static readonly TimedMapCollection<string, bool> _arQuestTargetMap = new(DefaultArCacheLimit);

        #endregion

        #region Properties

        public ProtoProcessorOptionsConfig Options { get; }

        #endregion

        #region Constructor

        public ProtoProcessorService(
            ILogger<IProtoProcessorService> logger,
            IOptions<ProtoProcessorOptionsConfig> options,
            IAsyncQueue<ProtoPayloadQueueItem> protoQueue,
            IAsyncQueue<DataQueueItem> dataQueue,
            IGrpcClient<Payload.PayloadClient, PayloadRequest, PayloadResponse> grpcProtoClient,
            IGrpcClient<Leveling.LevelingClient, TrainerInfoRequest, TrainerInfoResponse> grpcLevelingClient)
        {
            _logger = logger;
            _protoQueue = protoQueue;
            _dataQueue = dataQueue;
            _grpcProtoClient = grpcProtoClient;
            _grpcLevelingClient = grpcLevelingClient;

            Options = options.Value;
        }

        #endregion

        #region Background Service

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                $"{nameof(IProtoProcessorService)} is now running in the background.");

            await Task.Run(async () =>
                await BackgroundProcessing(stoppingToken)
            , stoppingToken);
        }

        private async Task BackgroundProcessing(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (_protoQueue.Count == 0)
                {
                    Thread.Sleep(1);
                    //await Task.Delay(1, stoppingToken);
                    continue;
                }

                try
                {
                    var workItems = await _protoQueue.DequeueBulkAsync(Options.Queue.MaximumBatchSize, stoppingToken);
                    if (!(workItems?.Any() ?? false))
                    {
                        Thread.Sleep(1);
                        //await Task.Delay(1, stoppingToken);
                        continue;
                    }

                    //Parallel.ForEach(workItems, async payload => await ProcessWorkItemAsync(payload, stoppingToken).ConfigureAwait(false));
                    await Task.Run(() =>
                    {
                        new Thread(async () =>
                        {
                            foreach (var workItem in workItems)
                            {
                                await Task.Factory.StartNew(async () => await ProcessWorkItemAsync(workItem, stoppingToken));
                            }
                        })
                        { IsBackground = true }.Start();
                    }, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Prevent throwing if stoppingToken was signaled
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred executing task work item.");
                }

                Thread.Sleep(1);
                //await Task.Delay(50, stoppingToken);
            }

            _logger.LogError("Exited ProtoProcessorService background processing...");
        }

        private async Task ProcessWorkItemAsync(ProtoPayloadQueueItem payload, CancellationToken stoppingToken)
        {
            if (payload?.Payload == null || payload?.Device == null)
                return;

            CheckQueueLength();

            var sw = new Stopwatch();
            sw.Start();

            var uuid = payload.Payload.Uuid;
            var username = payload.Payload.Username;
            var level = payload.Payload.Level;
            var xp = payload.Payload.TrainerXp ?? 0;
            var timestamp = payload.Payload.Timestamp ?? DateTime.UtcNow.ToTotalSeconds();
            var hasArQuestReqGlobal = payload.Payload.HaveAr;

            var isMadData = false;
            //var isEmptyGmo = true;
            //var isInvalidGmo = true;
            //var containsGmo = false;

            //Coordinate? targetCoord = null;
            //var inArea = false;
            //if (payload.Payload.LatitudeTarget != 0 && payload.Payload.LongitudeTarget != 0)
            //{
            //    targetCoord = new Coordinate(payload.Payload.LatitudeTarget, payload.Payload.LongitudeTarget);
            //}
            //var targetKnown = false;
            //S2CellId targetCellId = default;
            //if (targetCoord != null)
            //{
            //    // Check target is within cell id instead of checking geofences
            //    targetKnown = true;
            //    targetCellId = targetCoord.S2CellIdFromCoordinate();
            //    //_logger.LogDebug($"[{uuid}] Data received within target area {targetCoord} and target distance {payload.TargetMaxDistance}");
            //}
            //_logger.LogWarning($"[{device.Uuid}] InArea={inArea}");

            var processedProtos = new List<dynamic>();
            var contents = payload?.Payload?.Contents ?? new List<ProtoData>();
            foreach (var rawData in contents)
            {
                var data = rawData.Data;
                var method = (Method)rawData.Method;
                var hasArQuestReq = rawData.HaveAr;

                if (string.IsNullOrEmpty(data))
                {
                    _logger.LogDebug($"[{uuid}] Unhandled proto {method} ({rawData.Method}): Proto data is null '{data}'");
                    continue;
                }

                switch (method)
                {
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
                                    foreach (var quest in quests!)
                                    {
                                        if (quest.QuestContext == QuestProto.Types.Context.ChallengeQuest &&
                                            quest.QuestType == QuestType.QuestGeotargetedArScan)
                                        {
                                            _arQuestActualMap.SetValue(uuid, value: true, timestamp);
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
                                        ?? GetArQuestMode(uuid!, timestamp);
                                    var title = fsr.ChallengeQuest.QuestDisplay.Title;
                                    var quest = fsr.ChallengeQuest.Quest;
                                    if (quest.QuestType == QuestType.QuestGeotargetedArScan && uuid != null)
                                    {
                                        _arQuestActualMap.SetValue(uuid, value: true, timestamp);
                                    }
                                    processedProtos.Add(new
                                    {
                                        type = ProtoDataType.Quest,
                                        title,
                                        quest,
                                        hasAr,
                                    });
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
                        try
                        {
                            //isEmptyGmo = false;
                            //isInvalidGmo = false;
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
                        if (Options.ProcessMapPokemon && (level >= 30 || isMadData))
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
                        var gmo = ParseGetMapObjectsProto(uuid!, username!, data);
                        if (gmo.Any())
                        {
                            processedProtos.AddRange(gmo);
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
                // Inform frontend/configurator via RPC of trainer account XP and Level for leveling instance stats
                await _grpcProtoClient.SendAsync(new PayloadRequest
                {
                    PayloadType = PayloadType.PlayerInfo,
                    Payload = playerInfo.ToJson(),
                    Username = username,
                });
            }

            sw.Stop();

            if (!processedProtos.Any())
                return;

            if (string.IsNullOrEmpty(username))
                return;

            var storeData = await IsAllowedToSaveDataAsync(username);
            if (!storeData)
                return;

            // TODO: Make parsed protos logging configurable
            //var totalSeconds = Math.Round(sw.Elapsed.TotalSeconds, 4);
            //_logger.LogInformation($"[{uuid}] {processedProtos.Count:N0} protos parsed in {totalSeconds}s");

            ProtoDataStatistics.Instance.TotalProtosProcessed += (uint)processedProtos.Count;
            await _dataQueue.EnqueueAsync(new DataQueueItem
            {
                Username = username,
                Data = processedProtos,
            }, stoppingToken);
        }

        #endregion

        #region AR Quest Caching

        public static void SetArQuestTarget(string uuid, ulong timestamp, bool isAr)
        {
            _arQuestTargetMap.SetValue(uuid, isAr, timestamp);
            if (isAr)
            {
                // AR mode is sent to client -> client will clear ar quest
                _arQuestActualMap.SetValue(uuid, false, timestamp);
            }
        }

        private static bool GetArQuestMode(string uuid, ulong timestamp)
        {
            if (string.IsNullOrEmpty(uuid))
                return true;

            var actualMode = _arQuestActualMap.GetValueAt(uuid, timestamp) ?? false;
            var targetMode = _arQuestTargetMap.GetValueAt(uuid, timestamp);
            if (targetMode == null)
                return true;
            else if (targetMode ?? false)
                return false;

            return actualMode;
        }

        #endregion

        #region Proto Parsing Methods

        private IEnumerable<dynamic> ParseGetMapObjectsProto(string uuid, string username, string payload)
        {
            var results = new List<dynamic>();
            try
            {
                //containsGmo = true;
                var gmo = GetMapObjectsOutProto.Parser.ParseFrom(Convert.FromBase64String(payload));
                if (gmo == null)
                {
                    return results;
                }

                //isInvalidGmo = false;
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
                    return results;
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
                        if (Options.ProcessMapPokemon && fort.ActivePokemon != null)
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
                        ulong cellId = Convert.ToUInt64(cell.cell);
                        _emptyCells.AddOrUpdate(cellId, 1, (key, oldValue) => ++oldValue);

                        if (_emptyCells[cellId] == DefaultMaxEmptyCellsCount)
                        {
                            _logger.LogWarning($"[{uuid}] Cell {cellId} was empty 3 times in a row. Assuming empty.");
                            results.Add(cell);
                        }
                    }

                    //isEmptyGmo = true;
                    _logger.LogDebug($"[{uuid}] GMO is empty.");
                }
                else
                {
                    foreach (var cell in newCells)
                    {
                        ulong cellId = Convert.ToUInt64(cell.cell);
                        _emptyCells.AddOrUpdate(cellId, 0, (key, oldValue) => 0);
                    }

                    //isEmptyGmo = false;
                }

                results.AddRange(newCells);
                results.AddRange(newClientWeather);
                results.AddRange(newForts);
                results.AddRange(newWildPokemon);
                results.AddRange(newNearbyPokemon);
                results.AddRange(newMapPokemon);
            }
            catch (Exception ex)
            {
                _logger.LogError($"[{uuid}] Unable to decode GetMapObjectsOutProto: {ex}");
            }
            return results;
        }

        #endregion

        #region Private Methods

        private async Task<bool> IsAllowedToSaveDataAsync(string username)
        {
            if (_canStoreData.ContainsKey(username))
            {
                return _canStoreData[username];
            }

            // Get trainer leveling status from JobControllerService using gRPC and whether we should store the data or not
            var levelingStatus = await _grpcLevelingClient.SendAsync(new TrainerInfoRequest
            {
                Username = username,
            });
            if ((levelingStatus?.Status ?? TrainerInfoStatus.Error) != TrainerInfoStatus.Ok)
            {
                // Failure occurred, return true to be safe
                return true;
            }

            // Only store data if trainer is not currently leveling or if trainer is leveling and instance is configured
            // to store leveling data found
            var storeData = 
                (levelingStatus!.IsLeveling && levelingStatus!.StoreLevelingData) ||
                !levelingStatus!.IsLeveling;

            _canStoreData.AddOrUpdate(username, storeData, (key, oldValue) => storeData);

            return storeData;
        }

        private void CheckQueueLength()
        {
            var usage = $"{_protoQueue.Count:N0}/{Options.Queue.MaximumCapacity:N0}";
            if (_protoQueue.Count >= Options.Queue.MaximumCapacity)
            {
                _logger.LogError($"Proto processing queue is at maximum capacity! {usage}");
            }
            else if (_protoQueue.Count >= Options.Queue.MaximumSizeWarning)
            {
                _logger.LogWarning($"Proto processing queue is over normal capacity with {usage} items total, consider increasing 'MaximumQueueBatchSize'");
            }
        }

        #endregion
    }
}