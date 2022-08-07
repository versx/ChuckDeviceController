namespace ChuckDeviceController.Controllers
{
    using System;

    using Microsoft.AspNetCore.Mvc;
    using POGOProtos.Rpc;

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

        private readonly ILogger<ProtoController> _logger;
        private readonly Data.Contexts.ControllerContext _context;
        private readonly IProtoProcessorService _protoProcessor;

        #endregion

        #region Constructor

        public ProtoController(
            ILogger<ProtoController> logger,
            Data.Contexts.ControllerContext context,
            IProtoProcessorService protoProcessor)
        {
            _logger = logger;
            _context = context;
            _protoProcessor = protoProcessor;
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
        public async Task<ProtoResponse?> PostAsync(ProtoPayload payload)
        {
            var response = await HandleProtoRequest(payload).ConfigureAwait(false);
            if (response?.Data == null)
            {
                //_logger.LogError($"[{payload.Uuid}] null data response!");
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

        #region Request Handlers

        private async Task<ProtoResponse?> HandleProtoRequest(ProtoPayload payload)
        {
            if (payload == null)
            {
                _logger.LogError("Invalid proto payload received");
                return null;
            }

            var now = DateTime.UtcNow.ToTotalSeconds();
            var device = await _context.Devices.FindAsync(payload.Uuid);
            if (device != null)
            {
                device.LastLatitude = payload.LatitudeTarget;
                device.LastLongitude = payload.LongitudeTarget;
                device.LastSeen = now;

                _context.Update(device);
                await _context.SaveChangesAsync();
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
                await _context.SaveChangesAsync();
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
                        var account = await _context.Accounts.FindAsync(payload.Username);
                        if (account != null)
                        {
                            account.Level = payload.Level;
                            await _context.SaveChangesAsync();
                            _logger.LogInformation($"Account {payload.Username} on {payload.Uuid} from {oldLevel} to {payload.Level} with {payload.TrainerXp}");
                        }
                        _levelCache[payload.Username] = payload.Level;
                    }
                }
            }

            if ((payload.Contents?.Count ?? 0) == 0)
            {
                _logger.LogWarning($"[{payload.Uuid}] Invalid or empty GMO");
                return null;
            }

            // Queue proto payload for processing
            await _protoProcessor.EnqueueAsync(new ProtoPayloadQueueItem
            {
                Payload = payload,
                Device = device,
            });

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