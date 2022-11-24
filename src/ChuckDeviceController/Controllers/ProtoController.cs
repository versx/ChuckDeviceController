namespace ChuckDeviceController.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using POGOProtos.Rpc;

    using ChuckDeviceController.Collections.Queues;
    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Extensions;
    using ChuckDeviceController.Extensions.Http.Caching;
    using ChuckDeviceController.Net.Models.Requests;
    using ChuckDeviceController.Net.Models.Responses;
    using ChuckDeviceController.Services;

    [ApiController]
    public class ProtoController : ControllerBase
    {
        #region Variables

        private static readonly SemaphoreSlim _semDevices = new(1);
        private static readonly Dictionary<string, ushort> _levelCache = new();
        private static readonly object _levelCacheLock = new();

        private readonly ILogger<ProtoController> _logger;
        private readonly IAsyncQueue<ProtoPayloadQueueItem> _taskQueue;
        private readonly IMemoryCacheHostedService _memCache;
        private readonly ControllerDbContext _context;

        #endregion

        #region Constructor

        public ProtoController(
            ILogger<ProtoController> logger,
            IAsyncQueue<ProtoPayloadQueueItem> taskQueue,
            IMemoryCacheHostedService memCache,
            ControllerDbContext context)
        {
            _logger = logger;
            _taskQueue = taskQueue;
            _memCache = memCache;
            _context = context;
        }

        #endregion

        #region Routes

        // Test route for debugging, TODO: remove in production
#if DEBUG
        [HttpGet("/raw")]
        public string Get() => ":D";
#endif

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
            await _taskQueue.EnqueueAsync(new ProtoPayloadQueueItem
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

        private async Task<Device?> SetLastDeviceLocationAsync(ProtoPayload payload)
        {
            await _semDevices.WaitAsync();

            try
            {
                var now = DateTime.UtcNow.ToTotalSeconds();
                var device = await _context.Devices.FindAsync(payload.Uuid);
                if (device == null)
                {
                    device = new Device
                    {
                        Uuid = payload.Uuid,
                        AccountUsername = payload.Username,
                        LastLatitude = payload.LatitudeTarget,
                        LastLongitude = payload.LongitudeTarget,
                        LastSeen = now,
                    };
                    await _context.Devices.AddAsync(device);
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
                        _semDevices.Release();
                        return device;
                    }

                    device.LastLatitude = payload.LatitudeTarget;
                    device.LastLongitude = payload.LongitudeTarget;
                    device.LastSeen = now;
                    _context.Devices.Update(device);
                }

                await _context.SaveChangesAsync();

                _semDevices.Release();
                return device;
            }
            catch (Exception ex)
            {
                _logger.LogError($"SetLastDeviceLocationAsync: {ex}");
            }

            _semDevices.Release();
            return null;
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

                    if (level < oldLevel)
                        return;

                    // Account level has changed, update cache
                    _levelCache[username] = level;
                }

                // Update account level if account exists
                var account = await _context.Accounts.FindAsync(username);
                if (account != null)
                {
                    account.Level = level;
                    _context.Accounts.Update(account);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"[{uuid}] Account '{username}' on device '{uuid}' went from level {oldLevel} to {level} with {trainerXp:N0} XP");
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