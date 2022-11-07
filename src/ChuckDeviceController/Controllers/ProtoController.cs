namespace ChuckDeviceController.Controllers
{
    using System.Collections.Concurrent;
    using System.Timers;

    using Microsoft.AspNetCore.Mvc;
    using POGOProtos.Rpc;

    using ChuckDeviceController.Collections.Queues;
    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Data.Repositories;
    using ChuckDeviceController.Extensions;
    using ChuckDeviceController.Extensions.Http.Caching;
    using ChuckDeviceController.Net.Models.Responses;
    using ChuckDeviceController.Net.Models.Requests;
    using ChuckDeviceController.Services;

    [ApiController]
    public class ProtoController : ControllerBase
    {
        private const int DevicesUpdateIntervalS = 10;

        #region Variables

        private static readonly ConcurrentBag<Device> _devicesToUpdate = new();
        private static readonly object _devicesLock = new();

        private static readonly Dictionary<string, ushort> _levelCache = new();
        private static readonly object _levelCacheLock = new();

        private readonly ILogger<ProtoController> _logger;
        private readonly ControllerDbContext _context;
        private readonly IAsyncQueue<ProtoPayloadQueueItem> _taskQueue;
        private readonly IMemoryCacheHostedService _memCache;
        private readonly Timer _timer;
        private readonly string _connectionString;
        private readonly SqlBulk _bulk;

        #endregion

        #region Constructor

        public ProtoController(
            ILogger<ProtoController> logger,
            ControllerDbContext context,
            IAsyncQueue<ProtoPayloadQueueItem> taskQueue,
            IConfiguration configuration,
            IMemoryCacheHostedService memCache)
        {
            _logger = logger;
            _context = context;
            _taskQueue = taskQueue;
            _memCache = memCache;
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _bulk = new SqlBulk(_connectionString);

            _timer = new()
            {
                Interval = DevicesUpdateIntervalS * 1000,
            };
            _timer.Elapsed += async (sender, e) => await UpdateDevicesAsync();
            _timer.Start();
        }

        #endregion

        #region Routes

        // Test route for debugging, TODO: remove in production
        [HttpGet("/raw")]
        public string Get() => ":D";

        // Handle incoming raw proto data
        [
            HttpPost("/raw"),
            Produces("application/json"),
        ]
        public async Task<ProtoResponse?> PostAsync(ProtoPayload payload)
        {
            Response.Headers["Accept"] = "application/json";
            Response.Headers["Content-Type"] = "application/json";

            ProtoDataStatistics.Instance.TotalRequestsProcessed++;

            var response = await HandleProtoRequest(payload).ConfigureAwait(false);
            return response;
        }

        [HttpGet("/stats")]
        public async Task<ActionResult> GetStatsAsync()
        {
            var json = new JsonResult(new
            {
                total_requests = ProtoDataStatistics.Instance.TotalRequestsProcessed,
                protos_received = ProtoDataStatistics.Instance.TotalProtoPayloadsReceived,
                protos_processed = ProtoDataStatistics.Instance.TotalProtosProcessed,
                entities_processed = ProtoDataStatistics.Instance.TotalEntitiesProcessed,
                entities_upserted = ProtoDataStatistics.Instance.TotalEntitiesUpserted,
                data_times = new
                {
                    average_insert_count = ProtoDataStatistics.Instance.AverageTime.Count,
                    average_insert_seconds = ProtoDataStatistics.Instance.AverageTime.TimeS,
                    total_collected_benchmark_times = ProtoDataStatistics.Instance.Times.Count,
                },
            });
            return await Task.FromResult(json);
        }

        #endregion

        #region Request Handlers

        private async Task<ProtoResponse?> HandleProtoRequest(ProtoPayload payload)
        {
            if (payload == null)
            {
                _logger.LogError("Invalid proto payload received");
                return null;
            }

            // Check if received payload data is empty, if so skip
            if (!(payload.Contents?.Any() ?? false))
            {
                _logger.LogWarning($"[{payload.Uuid}] Invalid or empty GMO");
                return null;
            }

            // Cache account level and update account if level changed
            await SetAccountLevelAsync(payload.Uuid, payload.Username, payload.Level, payload.TrainerXp ?? 0);

            // Set device last location and last seen time
            var device = await SetLastDeviceLocationAsync(payload);

            // Queue proto payload for processing
            _taskQueue.Enqueue(new ProtoPayloadQueueItem
            {
                Payload = payload,
                Device = device,
            });
            ProtoDataStatistics.Instance.TotalProtoPayloadsReceived++;

            var response = BuildProtoResponse(payload);
            return response;
        }

        #endregion

        #region Private Methods

        private async Task<Device> SetLastDeviceLocationAsync(ProtoPayload payload)
        {
            var device =  await EntityRepository.GetEntityAsync<string, Device, ControllerDbContext>(_context, _memCache, payload.Uuid);
            if (device == null)
            {
                device = new Device
                {
                    Uuid = payload.Uuid,
                    AccountUsername = payload.Username,
                };
            }
            else
            {
                var deviceLat = Math.Round(device.LastLatitude ?? 0, 5);
                var deviceLon = Math.Round(device.LastLongitude ?? 0, 5);
                var payloadLat = Math.Round(payload.LatitudeTarget, 5);
                var payloadLon = Math.Round(payload.LongitudeTarget, 5);
                if (deviceLat == payloadLat &&
                    deviceLon == payloadLon)
                {
                    // At same location, no need to update
                    // TODO: Should update last_seen (maybe)
                    return device;
                }
            }

            var now = DateTime.UtcNow.ToTotalSeconds();
            device.LastLatitude = payload.LatitudeTarget;
            device.LastLongitude = payload.LongitudeTarget;
            device.LastSeen = now;

            lock (_devicesLock)
            {
                _devicesToUpdate.Add(device);
            }

            return device;
        }

        private async Task UpdateDevicesAsync()
        {
            List<Device> devices;
            lock (_devicesLock)
            {
                if (_devicesToUpdate.Any())
                    return;

                devices = new List<Device>(_devicesToUpdate);
                _devicesToUpdate.Clear();
            }

            if (!(devices?.Any() ?? false))
            {
                return;
            }

            try
            {
                await _bulk.InsertInBulkAsync(
                    SqlQueries.DeviceOnMergeUpdate,
                    SqlQueries.DeviceValues,
                    devices
                );
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error: {ex}");
            }
        }

        private async Task SetAccountLevelAsync(string uuid, string? username, ushort level, ulong trainerXp = 0)
        {
            try
            {
                if (string.IsNullOrEmpty(username) || level <= 0)
                    return;

                ushort oldLevel;
                lock (_levelCacheLock)
                {
                    // Check if account level has already been cached
                    if (!_levelCache.ContainsKey(username))
                    {
                        _levelCache.Add(username, level);
                        return;
                    }

                    // Check if cached level is same as current level
                    oldLevel = _levelCache[username];
                    if (oldLevel == level)
                        return;

                    // Account level has changed, update cache
                    _levelCache[username] = level;
                }

                // Update account level if account exists
                var account = await _context.Accounts.FindAsync(username);
                if (account != null)
                {
                    account.Level = level;
                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"[{uuid}] Account {username} on {uuid} from {oldLevel} to {level} with {trainerXp}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[{uuid}] Error: {ex}");
            }
        }

        private static ProtoResponse BuildProtoResponse(ProtoPayload payload)
        {
            var hasGmo = payload.Contents?.Any(content => content.Method == (int)Method.GetMapObjects) ?? false;
            return new ProtoResponse
            {
                Status = "ok",
                // NOTE: Provide actual response details, pretty sure most of it doesn't matter though
                Data = new ProtoDataDetails
                {
                    InArea = true,
                    ContainsGmos = hasGmo,
                    Encounters = payload.Contents?.Count(content => content.Method == (int)Method.Encounter) ?? 0,
                    Forts = 1,
                    FortSearch = payload.Contents?.Count(content => content.Method == (int)Method.FortSearch) ?? 0,
                    LatitudeTarget = payload.LatitudeTarget,
                    LongitudeTarget = payload.LongitudeTarget,
                    Level = payload.Level,
                    Nearby = 1,
                    OnlyEmptyGmos = !hasGmo,
                    OnlyInvalidGmos = !hasGmo,
                    Quests = 1,
                    Wild = 1,
                },
            };
        }

        #endregion
    }
}