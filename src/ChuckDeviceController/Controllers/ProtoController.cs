namespace ChuckDeviceController.Controllers
{
    using System;

    using Microsoft.AspNetCore.Mvc;
    using POGOProtos.Rpc;

    using ControllerContext = ChuckDeviceController.Data.Contexts.ControllerContext;
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

        private readonly ILogger<ProtoController> _logger;
        private readonly ControllerContext _context;
        private readonly IProtoProcessorService _protoProcessor;

        #endregion

        #region Constructor

        public ProtoController(
            ILogger<ProtoController> logger,
            ControllerContext context,
            IProtoProcessorService protoProcessor)
        {
            _logger = logger;
            _context = context;
            _protoProcessor = protoProcessor;
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

            // Set device last location and last seen time
            var device = await SetLastDeviceLocationAsync(payload);

            // Cache account level and update account if level changed
            await SetAccountLevelAsync(payload.Uuid, payload.Username, payload.Level, payload.TrainerXp ?? 0);

            // Queue proto payload for processing
            await _protoProcessor.EnqueueAsync(new ProtoPayloadQueueItem
            {
                Payload = payload,
                Device = device,
            });

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

                _context.Update(device);
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
                await _context.AddAsync(device);
            }
            await _context.SaveChangesAsync();

            return device;
        }

        private async Task SetAccountLevelAsync(string uuid, string? username, ushort level, ulong trainerXp = 0)
        {
            if (string.IsNullOrEmpty(username) || level <= 0)
                return;

            // Check if account level has already been cached
            if (!_levelCache.ContainsKey(username))
            {
                _levelCache.Add(username, level);
                return;
            }

            // Check if cached level is same as current level
            var oldLevel = _levelCache[username];
            if (oldLevel == level)
                return;

            // Account level has changed, update cache
            _levelCache[username] = level;

            // Update account level if account exists
            var account = await _context.Accounts.FindAsync(username);
            if (account != null)
            {
                account.Level = level;
                await _context.SaveChangesAsync();

                _logger.LogInformation($"[{uuid}] Account {username} on {uuid} from {oldLevel} to {level} with {trainerXp}");
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