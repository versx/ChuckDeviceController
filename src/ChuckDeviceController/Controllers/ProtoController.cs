namespace ChuckDeviceController.Controllers
{
    using System;
    using System.Diagnostics;

    using Microsoft.AspNetCore.Mvc;

    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Extensions;
    using ChuckDeviceController.Net.Models.Responses;
    using ChuckDeviceController.Net.Models.Requests;
    using ChuckDeviceController.Services;

    [ApiController]
    public class ProtoController : ControllerBase
    {
        private readonly ILogger<ProtoController> _logger;

        private readonly Dictionary<string, ushort> _levelCache;

        private readonly DeviceControllerContext _context;
        private readonly IProtoProcessorService _protoProcessor;

        #region Constructor

        public ProtoController(
            ILogger<ProtoController> logger,
            DeviceControllerContext context,
            IProtoProcessorService protoProcessor)
        {
            _logger = logger;
            _context = context;
            _protoProcessor = protoProcessor;

            _levelCache = new Dictionary<string, ushort>();
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

        private async Task<ProtoResponse> HandleProtoRequest(ProtoPayload payload)
        {
            if (payload == null)
            {
                _logger.LogError("Invalid proto payload received");
                return null;
            }

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var device = await _context.Devices.FindAsync(payload.Uuid);
            if (device != null)
            {
                device.LastLatitude = payload.LatitudeTarget;
                device.LastLongitude = payload.LongitudeTarget;
                device.LastSeen = DateTime.UtcNow.ToTotalSeconds();
                await _context.SaveChangesAsync();
            }
            else
            {
                // TODO: Remove dev code
                device = new Device
                {
                    Uuid = payload.Uuid,
                    AccountUsername = payload.Username,
                    InstanceName = "",
                    LastHost = "127.0.0.1",
                    LastLatitude = payload.LatitudeTarget,
                    LastLongitude = payload.LongitudeTarget,
                    LastSeen = DateTime.UtcNow.ToTotalSeconds(),
                };
                await _context.SaveChangesAsync();
            }
            /*
            var device = await _deviceRepository.GetByIdAsync(payload.Uuid).ConfigureAwait(false);
            try
            {
                if (device != null)
                {
                    device.LastLatitude = payload.LatitudeTarget;
                    device.LastLongitude = payload.LongitudeTarget;
                    device.LastSeen = DateTime.UtcNow.ToTotalSeconds();
                    await _deviceRepository.UpdateAsync(device).ConfigureAwait(false);
                }
                else
                {
                    device = await _deviceRepository.AddOrUpdateAsync(new Device
                    {
                        Uuid = payload.Uuid,
                        AccountUsername = payload.Username,
                        InstanceName = "",
                        LastHost = "127.0.0.1",
                        LastLatitude = payload.LatitudeTarget,
                        LastLongitude = payload.LongitudeTarget,
                        LastSeen = DateTime.UtcNow.ToTotalSeconds(),
                    }).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error: {ex}");
            }
            */

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
                        /*
                        var account = await _accountRepository.GetByIdAsync(payload.Username).ConfigureAwait(false);
                        if (account != null)
                        {
                            account.Level = payload.Level;
                            await _accountRepository.UpdateAsync(account).ConfigureAwait(false);
                        }
                        */
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
            await _protoProcessor.EnqueueAsync(new ProtoPayloadItem
            {
                Payload = payload,
                Device = device,
            });

            return new ProtoResponse
            {
                Status = "ok",
                Data = new ProtoDataDetails(),
            };
        }

        #endregion
    }
}