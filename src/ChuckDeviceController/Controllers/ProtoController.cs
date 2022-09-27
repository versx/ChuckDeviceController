namespace ChuckDeviceController.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using POGOProtos.Rpc;

    using ChuckDeviceController.Collections.Queues;
    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Extensions;
    using ChuckDeviceController.Net.Models.Responses;
    using ChuckDeviceController.Net.Models.Requests;
    using ChuckDeviceController.Services;

    [ApiController]
    public class ProtoController : ControllerBase
    {
        #region Variables

        private static readonly Dictionary<string, ushort> _levelCache = new();
        private static readonly object _levelCacheLock = new();

        private readonly ILogger<ProtoController> _logger;
        private readonly ControllerDbContext _context;
        private readonly IAsyncQueue<ProtoPayloadQueueItem> _taskQueue;

        #endregion

        #region Constructor

        public ProtoController(
            ILogger<ProtoController> logger,
            ControllerDbContext context,
            IAsyncQueue<ProtoPayloadQueueItem> taskQueue)
        {
            _logger = logger;
            _context = context;
            _taskQueue = taskQueue;
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
        public async Task<ActionResult> GetStats()
        {
            var json = new JsonResult(new
            {
                requests_per_second = ProtoDataStatistics.Instance.TotalRequestsProcessed,
                protos_received = ProtoDataStatistics.Instance.TotalProtoPayloadsReceived,
                protos_processed = ProtoDataStatistics.Instance.TotalProtosProcessed,
                entities_processed = ProtoDataStatistics.Instance.TotalEntitiesProcessed,
                entities_upserted = ProtoDataStatistics.Instance.TotalEntitiesUpserted,
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

            // Set device last location and last seen time
            var device = await SetLastDeviceLocationAsync(payload);

            // Cache account level and update account if level changed
            await SetAccountLevelAsync(payload.Uuid, payload.Username, payload.Level, payload.TrainerXp ?? 0);

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
            var now = DateTime.UtcNow.ToTotalSeconds();
            var device = await _context.Devices.FindAsync(payload.Uuid);
            if (device != null)
            {
                device.LastLatitude = payload.LatitudeTarget;
                device.LastLongitude = payload.LongitudeTarget;
                device.LastSeen = now;
            }
            else
            {
                device = new Device
                {
                    Uuid = payload.Uuid,
                    AccountUsername = payload.Username,
                    LastLatitude = payload.LatitudeTarget,
                    LastLongitude = payload.LongitudeTarget,
                    LastSeen = now,
                };
            }
            await _context.Devices.SingleMergeAsync(device, options =>
            {
                options.UseTableLock = true;
                options.OnMergeUpdateInputExpression = p => new
                {
                    p.AccountUsername,
                    p.LastLatitude,
                    p.LastLongitude,
                    p.LastSeen,
                };
            });

            return device;
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