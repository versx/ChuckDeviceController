namespace ChuckDeviceController.Services.ProtoProcessor;

using System.Collections.Concurrent;
using System.Diagnostics;

using Microsoft.Extensions.Options;
using POGOProtos.Rpc;

using ChuckDeviceController.Collections;
using ChuckDeviceController.Collections.Cache;
using ChuckDeviceController.Configuration;
using ChuckDeviceController.Extensions;
using ChuckDeviceController.Extensions.Json;
using ChuckDeviceController.HostedServices;
using ChuckDeviceController.Net.Models.Requests;
using ChuckDeviceController.Protos;
using ChuckDeviceController.Services;
using ChuckDeviceController.Services.DataProcessor;

public class ProtoProcessorService : TimedHostedService, IProtoProcessorService
{
    #region Constants

    private const ushort DefaultMaxEmptyCellsCount = 3;
    private const uint DefaultArCacheLimit = 1000;
    private const int DefaultProcessingWaitTimeS = 3;

    #endregion

    #region Variables

    private readonly ILogger<IProtoProcessorService> _logger;
    private readonly SafeCollection<ProtoPayloadQueueItem> _protoQueue;
    private readonly SafeCollection<DataQueueItem> _dataQueue;
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
        SafeCollection<ProtoPayloadQueueItem> protoQueue,
        SafeCollection<DataQueueItem> dataQueue,
        IGrpcClient<Payload.PayloadClient, PayloadRequest, PayloadResponse> grpcProtoClient,
        IGrpcClient<Leveling.LevelingClient, TrainerInfoRequest, TrainerInfoResponse> grpcLevelingClient)
        : base(logger, options?.Value?.IntervalS ?? ProtoProcessorOptionsConfig.DefaultIntervalS)
    {
        _logger = logger;
        _protoQueue = protoQueue;
        _dataQueue = dataQueue;
        _grpcProtoClient = grpcProtoClient;
        _grpcLevelingClient = grpcLevelingClient;

        Options = options?.Value ?? new();
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

        await Task.CompletedTask;
    }

    protected override async Task RunJobAsync(CancellationToken stoppingToken)
    {
        if (_protoQueue.Count == 0)
        {
            return;
        }

        try
        {
            var workItems = _protoQueue.Take((int)Options.Queue.MaximumBatchSize);
            if (!(workItems?.Any() ?? false))
            {
                return;
            }

            //Parallel.ForEach(workItems, async payload => await ProcessWorkItemAsync(payload, stoppingToken).ConfigureAwait(false));
            new Thread(async () =>
            {
                foreach (var workItem in workItems)
                {
                    await ProcessWorkItemAsync(workItem, stoppingToken).ConfigureAwait(false);
                }
            })
            { IsBackground = true }.Start();
        }
        catch (OperationCanceledException)
        {
            // Prevent throwing if stoppingToken was signaled
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred executing task work item.");
        }

        await Task.CompletedTask;
    }

    private async Task ProcessWorkItemAsync(ProtoPayloadQueueItem payload, CancellationToken stoppingToken = default)
    {
        if (payload?.Payload == null || payload?.Device == null)
            return;

        CheckQueueLength();

        var sw = new Stopwatch();
        if (Options.ShowProcessingTimes)
        {
            sw.Start();
        }

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

        // TODO: Send gRPC/Rest request to fetch if controller for device IsEvent

        var processedProtos = new List<dynamic>();
        var contents = payload?.Payload?.Contents ?? new List<ProtoData>();
        foreach (var rawData in contents)
        {
            var data = rawData.Data;
            var method = (Method)rawData.Method;
            var hasArQuestReq = rawData.HaveAr;

            if (string.IsNullOrEmpty(data))
            {
                _logger.LogDebug("[{Uuid}] Unhandled proto {Method} ({Method}): Proto data is null '{Data}'", uuid, method, rawData.Method, data);
                continue;
            }

            switch (method)
            {
                case Method.GetPlayer:
                    try
                    {
                        var gpr = GetPlayerOutProto.Parser.ParseFrom(Convert.FromBase64String(data));
                        if (!(gpr?.Success ?? false))
                        {
                            _logger.LogError("[{Uuid}] Malformed GetPlayerOutProto", uuid);
                            continue;
                        }

                        processedProtos.Add(new
                        {
                            type = ProtoDataType.PlayerData,
                            gpr,
                            username,
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("[{Uuid}] Unable to decode GetPlayerOutProto: {Message}", uuid, ex.InnerException?.Message ?? ex.Message);
                    }
                    break;
                case Method.GetHoloholoInventory:
                    try
                    {
                        var ghi = GetHoloholoInventoryOutProto.Parser.ParseFrom(Convert.FromBase64String(data));
                        if (!(ghi?.Success ?? false))
                        {
                            _logger.LogError("[{Uuid}] Malformed GetHoloholoInventoryOutProto", uuid);
                            continue;
                        }

                        var inventoryItems = ghi.InventoryDelta.InventoryItem;
                        if (!inventoryItems.Any())
                            continue;

                        foreach (var item in inventoryItems)
                        {
                            var itemData = item.InventoryItemData;
                            if (itemData == null)
                                continue;

                            if ((itemData.PlayerStats?.Experience ?? 0) > 0)
                            {
                                xp = Convert.ToUInt64(itemData.PlayerStats!.Experience);
                            }

                            var quests = itemData.Quests?.Quest;
                            if (uuid == null || !(quests?.Any() ?? false))
                                continue;

                            foreach (var quest in quests)
                            {
                                if (quest.QuestContext == QuestProto.Types.Context.ChallengeQuest &&
                                    quest.QuestType == QuestType.QuestGeotargetedArScan)
                                {
                                    _arQuestActualMap.SetValue(uuid, value: true, timestamp);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("[{Uuid}] Unable to decode GetHoloholoInventoryOutProto: {Message}", uuid, ex.InnerException?.Message ?? ex.Message);
                    }
                    break;
                case Method.FortSearch:
                    try
                    {
                        var fsr = FortSearchOutProto.Parser.ParseFrom(Convert.FromBase64String(data));
                        if (fsr?.ChallengeQuest?.Quest == null)
                            continue;

                        // Check for AR quest or if AR quest is required
                        var hasAr = hasArQuestReqGlobal
                            ?? hasArQuestReq
                            ?? GetArQuestMode(uuid!, timestamp);
                        hasAr = false; // TODO: Remove
                        var title = fsr.ChallengeQuest.QuestDisplay.Title;
                        var quest = fsr.ChallengeQuest.Quest;

                        // Ignore AR quests so they get rescanned if they were the first quest a scanner would hold onto
                        if (quest.QuestType == QuestType.QuestGeotargetedArScan && !Options.AllowArQuests)
                        {
                            _logger.LogWarning("[{Uuid}] Quest was blocked because it is type '{QuestType}'.", uuid, quest.QuestType);
                            _logger.LogInformation("[{Uuid}] Quest info: {Quest}", uuid, quest);
                            continue;
                        }

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
                    catch (Exception ex)
                    {
                        _logger.LogError("[{Uuid}] Unable to decode FortSearchOutProto: {Message}", uuid, ex.InnerException?.Message ?? ex.Message);
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
                            var status = er?.Status ?? EncounterOutProto.Types.Status.EncounterError;
                            if (status != EncounterOutProto.Types.Status.EncounterSuccess)
                            {
                                _logger.LogError("[{Uuid}] Malformed EncounterOutProto", uuid);
                                continue;
                            }

                            processedProtos.Add(new
                            {
                                type = ProtoDataType.Encounter,
                                data = er,
                                username,
                                isEvent = false,
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("[{Uuid}] Unable to decode EncounterOutProto: {Message}", ex.InnerException?.Message ?? ex.Message);
                    }
                    break;
                case Method.DiskEncounter:
                    if (Options.ProcessMapPokemon && (level >= 30 || isMadData))
                    {
                        try
                        {
                            var der = DiskEncounterOutProto.Parser.ParseFrom(Convert.FromBase64String(data));
                            var status = der?.Result ?? DiskEncounterOutProto.Types.Result.Unknown;
                            if (status != DiskEncounterOutProto.Types.Result.Success)
                            {
                                _logger.LogError("[{Uuid}] Malformed DiskEncounterOutProto", uuid);
                                continue;
                            }

                            processedProtos.Add(new
                            {
                                type = ProtoDataType.DiskEncounter,
                                data = der,
                                username,
                                isEvent = false,
                            });
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError("[{Uuid}] Unable to decode DiskEncounterOutProto: {Message}", ex.InnerException?.Message ?? ex.Message);
                        }
                    }
                    break;
                case Method.FortDetails:
                    try
                    {
                        var fdr = FortDetailsOutProto.Parser.ParseFrom(Convert.FromBase64String(data));
                        if (fdr == null)
                        {
                            _logger.LogError("[{Uuid}] Malformed FortDetailsOutProto", uuid);
                            continue;
                        }

                        processedProtos.Add(new
                        {
                            type = ProtoDataType.FortDetails,
                            data = fdr,
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("[{Uuid}] Unable to decode FortDetailsOutProto: {Message}", ex.InnerException?.Message ?? ex.Message);
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
                        if (ggi == null)
                        {
                            _logger.LogError("[{Uuid}] Malformed GymGetInfoOutProto", uuid);
                            continue;
                        }

                        processedProtos.Add(new
                        {
                            type = ProtoDataType.GymInfo,
                            data = ggi,
                        });

                        if (!Options.ProcessGymDefenders && !Options.ProcessGymTrainers)
                            continue;

                        if (ggi.GymStatusAndDefenders == null)
                        {
                            _logger.LogWarning("[{Uuid}] Invalid GymStatusAndDefenders provided, skipping...\n: {Ggi}", uuid, ggi);
                            continue;
                        }
                        var fortId = ggi.GymStatusAndDefenders.PokemonFortProto.FortId;
                        var gymDefenders = ggi.GymStatusAndDefenders.GymDefender;
                        if (gymDefenders == null)
                            continue;

                        foreach (var gymDefender in gymDefenders)
                        {
                            if (Options.ProcessGymTrainers && gymDefender.TrainerPublicProfile != null)
                            {
                                processedProtos.Add(new
                                {
                                    type = ProtoDataType.Trainer,
                                    data = gymDefender.TrainerPublicProfile,
                                });
                            }
                            if (Options.ProcessGymDefenders && gymDefender.MotivatedPokemon != null)
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
                    catch (Exception ex)
                    {
                        _logger.LogError("[{Uuid}] Unable to decode GymGetInfoOutProto: {Message}", uuid, ex.InnerException?.Message ?? ex.Message);
                    }
                    break;
                case Method.Unset:
                default:
                    _logger.LogDebug("[{Uuid}] Invalid method or data provided. {Method}:{Data}", uuid, method, data);
                    break;
            }
        }

        if (!string.IsNullOrEmpty(username) && xp > 0 && level > 0)
        {
            // Inform frontend/configurator via RPC of trainer account XP and Level for leveling instance stats
            var json = new { username, xp, level }.ToJson();
            if (!string.IsNullOrEmpty(json))
            {
                await SendPlayerInfoAsync(username, json);
            }
        }

        if (!processedProtos.Any())
            return;

        if (string.IsNullOrEmpty(username))
            return;

        var storeData = await IsAllowedToSaveDataAsync(username);
        if (!storeData)
            return;

        if (Options.ShowProcessingTimes)
        {
            sw.Stop();
            var totalSeconds = Math.Round(sw.Elapsed.TotalSeconds, Options.DecimalPrecision);
            var time = Options.ShowProcessingTimes ? $" in {totalSeconds}s" : "";
            _logger.LogInformation("[{Uuid}] Parsed {Count:N0} protos{Time}", uuid, processedProtos.Count, time);
        }

        ProtoDataStatistics.Instance.TotalProtosProcessed += (uint)processedProtos.Count;
        if (!_dataQueue.TryAdd(new DataQueueItem
        {
            Username = username,
            Data = processedProtos,
        }))
        {
            // Failed to enqueue item with proto queue
            _logger.LogError($"Failed to enqueue entity data with data queue");
        }
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
            if (gmoMapCells.Count == 0)
            {
                _logger.LogDebug("[{Uuid}] Map cells are empty", uuid);
                return results;
            }

            var newWildPokemon = new List<dynamic>();
            var newNearbyPokemon = new List<dynamic>();
            var newMapPokemon = new List<dynamic>();
            var newClientWeather = new List<dynamic>();
            var newForts = new List<dynamic>();
            var newCells = new List<dynamic>();

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
                        isEvent = false,
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
                        _logger.LogWarning("[{Uuid}] Cell {CellId} was empty 3 times in a row. Assuming empty.", uuid, cellId);
                        results.Add(cell);
                    }
                }

                //isEmptyGmo = true;
                _logger.LogDebug("[{Uuid}] GMO is empty.", uuid);
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
            _logger.LogError("[{Uuid}] Unable to decode GetMapObjectsOutProto: {Message}", ex.InnerException?.Message ?? ex.Message);
        }
        return results;
    }

    #endregion

    #region Private Methods

    private async Task<bool> IsAllowedToSaveDataAsync(string username)
    {
        if (_canStoreData.TryGetValue(username, out var value))
        {
            return value;
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
            _logger.LogError("Proto processing queue is at maximum capacity! {Usage}", usage);
        }
        else if (_protoQueue.Count >= Options.Queue.MaximumSizeWarning)
        {
            _logger.LogWarning("Proto processing queue is over normal capacity with {Usage} items total, consider increasing 'MaximumQueueBatchSize'", usage);
        }
    }

    private async Task SendPlayerInfoAsync(string username, string json)
    {
        // Fire off gRPC request on a separate thread
        await Task.Run(() =>
        {
            new Thread(async () =>
            {
                // Send got trainer info proto message
                await _grpcProtoClient.SendAsync(new PayloadRequest
                {
                    PayloadType = PayloadType.PlayerInfo,
                    Payload = json,
                    Username = username,
                });
            })
            { IsBackground = true }.Start();
        });

        await Task.CompletedTask;
    }

    #endregion
}