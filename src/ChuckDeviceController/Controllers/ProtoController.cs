namespace ChuckDeviceController.Controllers
{
    using System.Collections.Concurrent;

    using Microsoft.AspNetCore.Mvc;
    using MySqlConnector;
    using POGOProtos.Rpc;

    using ChuckDeviceController.Collections;
    using ChuckDeviceController.Data;
    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Data.Repositories;
    using ChuckDeviceController.Extensions;
    using ChuckDeviceController.Extensions.Http;
    using ChuckDeviceController.Extensions.Http.Caching;
    using ChuckDeviceController.Net.Models.Requests;
    using ChuckDeviceController.Net.Models.Responses;
    using ChuckDeviceController.Services;

    [ApiController]
    public class ProtoController : ControllerBase
    {
        private const int DefaultConcurrencyLevel = 25;
        private const ushort DefaultCapacity = ushort.MaxValue;
        private const string ContentTypeJson = "application/json";

        #region Variables

        private static readonly ConcurrentDictionary<string, ushort> _levelCache = new(DefaultConcurrencyLevel, DefaultCapacity);
        private static readonly SemaphoreSlim _semDevices = new(DefaultConcurrencyLevel); // TODO: Make proto devices update concurrency level configurable

        private readonly ILogger<ProtoController> _logger;
        private readonly SafeCollection<ProtoPayloadQueueItem> _taskQueue;
        private readonly IMemoryCacheHostedService _memCache;
        private readonly MySqlConnection _connection;

        #endregion

        #region Constructor

        public ProtoController(
            ILogger<ProtoController> logger,
            SafeCollection<ProtoPayloadQueueItem> taskQueue,
            IMemoryCacheHostedService memCache,
            MySqlConnection connection)
        {
            _logger = logger;
            _taskQueue = taskQueue;
            _memCache = memCache;
            _connection = connection;
        }

        #endregion

        #region Routes

#if DEBUG
        // Test route for debugging
        [HttpGet("/raw")]
        public string Get() => ":D";
#endif

        // Handle incoming raw proto data
        [
            HttpPost("/raw"),
            Produces(ContentTypeJson),
        ]
        public async Task<ProtoResponse?> PostAsync(ProtoPayload payload)
        {
            Response.Headers["Accept"] = ContentTypeJson;
            Response.Headers["Content-Type"] = ContentTypeJson;

            // TODO: Use middleware
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
            var device = await SetDeviceLocationAsync(payload);

            // Queue proto payload for processing
            var wasAdded =  _taskQueue.TryAdd(new ProtoPayloadQueueItem
            {
                Payload = payload,
                Device = device,
            });
            if (!wasAdded)
            {
                // Failed to enqueue item with proto queue
                _logger.LogError($"[{payload.Uuid}] Failed to enqueue proto data with proto queue");
            }
            ProtoDataStatistics.Instance.TotalProtoPayloadsReceived++;

            var response = BuildProtoResponse(payload);
            return response;
        }

        #endregion

        #region Private Methods

        private async Task<Device?> SetDeviceLocationAsync(ProtoPayload payload)
        {
            await _semDevices.WaitAsync();
            Device? device = null;

            try
            {
                var now = DateTime.UtcNow.ToTotalSeconds();
                var ipAddr = Request.GetIPAddress(defaultValue: null);
                //\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b

                device = await EntityRepository.GetEntityAsync<string, Device>(_connection, payload.Uuid, _memCache, skipCache: false, setCache: true);
                if (device == null)
                {
                    device = new Device
                    {
                        Uuid = payload.Uuid,
                        AccountUsername = payload.Username,
                        LastHost = ipAddr,
                        LastLatitude = payload.LatitudeTarget,
                        LastLongitude = payload.LongitudeTarget,
                        LastSeen = now,
                    };
                }
                else
                {
                    var deviceLat = Math.Round(device.LastLatitude ?? 0, 6);
                    var deviceLon = Math.Round(device.LastLongitude ?? 0, 6);
                    var payloadLat = Math.Round(payload.LatitudeTarget, 6);
                    var payloadLon = Math.Round(payload.LongitudeTarget, 6);
                    if ((deviceLat != payloadLat || deviceLon != payloadLon) &&
                        payloadLat != 0 && payloadLon != 0)
                    {
                        device.LastLatitude = payloadLat;
                        device.LastLongitude = payloadLon;
                    }
                    if (device.LastHost != ipAddr)
                    {
                        device.LastHost = ipAddr;
                    }
                    device.LastSeen = now;
                }

                var sql = string.Format(SqlQueries.DeviceOnMergeUpdate, SqlQueries.DeviceValues);
                var result = await EntityRepository.ExecuteAsync(_connection, sql, device);
                if (result < 1)
                {
                    _logger.LogWarning($"Failed to update device '{device.Uuid}'");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"SetDeviceLastLocationAsync: {ex.InnerException?.Message ?? ex.Message}");
            }

            _semDevices.Release();
            return device;
        }

        private async Task SetAccountLevelAsync(string uuid, string? username, ushort level, ulong trainerXp = 0)
        {
            try
            {
                if (string.IsNullOrEmpty(username) || level <= 0)
                    return;

                // Attempt to get cached level, if it exists
                _levelCache.TryGetValue(username, out var oldLevel);

                // Check if cached level is same as current level
                // or if higher than current
                if (oldLevel == level || oldLevel > level)
                    return;

                // Add account level to cache, otherwise if it exists
                // update the value
                _levelCache.AddOrUpdate(username, level, (key, oldValue) => level);

                // Attempt to fetch account
                var account = await EntityRepository.GetEntityAsync<string, Account>(_connection, username, _memCache, skipCache: false, setCache: true);
                if (account == null)
                    return;

                if (account.Level != level)
                {
                    // Update account level
                    account.Level = level;
                    var result = await EntityRepository.ExecuteAsync(_connection, SqlQueries.AccountLevelUpdate, account);
                    if (result < 1)
                    {
                        _logger.LogWarning($"Failed to update level for account '{account.Username}'");
                    }
                }

                if (oldLevel > 0)
                {
                    _logger.LogInformation($"[{uuid}] Account '{username}' on device '{uuid}' leveled up from {oldLevel} to {level} with {trainerXp:N0} XP");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[{uuid}] Error: {ex.InnerException?.Message ?? ex.Message}");
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