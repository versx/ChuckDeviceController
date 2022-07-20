namespace ChuckDeviceConfigurator.Services.Rpc
{
    using Grpc.Core;
    using POGOProtos.Rpc;

    using ChuckDeviceConfigurator.Services.Jobs;
    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Extensions;
    using ChuckDeviceController.Protos;

    public class GrpcServerService : Payload.PayloadBase
    {
        #region Variables

        private readonly ILogger<GrpcServerService> _logger;
        private readonly IJobControllerService _jobControllerService;

        #endregion

        #region Constructor

        public GrpcServerService(
            ILogger<GrpcServerService> logger,
            IJobControllerService jobControllerService)
        {
            _logger = logger;
            _jobControllerService = jobControllerService;
        }

        #endregion

        public override Task<PayloadResponse> ReceivedPayload(PayloadRequest request, ServerCallContext context)
        {
            _logger.LogInformation($"Received {request.PayloadType} proto message");

            var json = request.Payload;
            if (string.IsNullOrEmpty(json))
            {
                _logger.LogError($"JSON payload was null, unable to deserialize Pokemon entity");
                return null;
            }

            switch (request.PayloadType)
            {
                case PayloadType.Pokemon:
                    var pokemon = json.FromJson<Pokemon>();
                    if (pokemon == null)
                    {
                        // Failed to deserialize payload to Pokemon
                        _logger.LogError($"Failed to deserialize JSON payload to Pokemon entity: {request.Payload}");
                        return null;
                    }

                    if (request.HasIV)
                    {
                        _jobControllerService.GotPokemonIV(pokemon);
                    }

                    _jobControllerService.GotPokemon(pokemon);
                    break;
                case PayloadType.Fort:
                    var fort = json.FromJson<PokemonFortProto>();
                    if (fort == null)
                    {
                        // Failed to deserialize payload to PokemonFortProto
                        _logger.LogError($"Failed to deserialize JSON payload to PokemonFortProto proto: {request.Payload}");
                        return null;
                    }

                    _jobControllerService.GotFort(fort, request.Username);
                    break;
                case PayloadType.PlayerData:
                    var data = json.FromJson<Dictionary<string, object>>();
                    if (data == null)
                    {
                        // Failed to deserialize payload to dictionary
                        _logger.LogError($"Failed to deserialize JSON payload to player data: {request.Payload}");
                        return null;
                    }

                    string username = Convert.ToString(data["username"]);
                    ushort level = Convert.ToUInt16(Convert.ToString(data["level"]));
                    ulong xp = Convert.ToUInt64(Convert.ToString(data["xp"]));

                    _jobControllerService.GotPlayerData(username, level, xp);
                    break;
            }

            return Task.FromResult(new PayloadResponse
            {
                Status = PayloadStatus.Ok,
            });
        }
    }
}