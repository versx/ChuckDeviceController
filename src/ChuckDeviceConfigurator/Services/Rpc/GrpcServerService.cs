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
                    HandlePokemonPayload(json, request.HasIV);
                    break;
                case PayloadType.PokemonList:
                    HandlePokemonListPayload(json, request.HasIV);
                    break;
                case PayloadType.Fort:
                    HandleFortPayload(json, request.Username);
                    break;
                case PayloadType.FortList:
                    HandleFortListPayload(json, request.Username);
                    break;
                case PayloadType.PlayerData:
                    HandlePlayerPayload(json);
                    break;
            }

            var response = new PayloadResponse
            {
                Status = PayloadStatus.Ok,
            };
            return Task.FromResult(response);
        }

        private void HandlePokemonPayload(string json, bool hasIV)
        {
            var pokemon = json.FromJson<Pokemon>();
            if (pokemon == null)
            {
                // Failed to deserialize payload to Pokemon
                _logger.LogError($"Failed to deserialize JSON payload to Pokemon entity: {json}");
                return;
            }

            if (hasIV)
            {
                _jobControllerService.GotPokemonIV(pokemon);
            }

            _jobControllerService.GotPokemon(pokemon);
        }

        private void HandlePokemonListPayload(string json, bool hasIV)
        {
            var pokemon = json.FromJson<List<Pokemon>>();
            if (pokemon == null)
            {
                // Failed to deserialize payload to list of PokemonFortProto
                _logger.LogError($"Failed to deserialize JSON payload to list of PokemonFortProto proto: {json}");
                return;
            }

            foreach (var pkmn in pokemon)
            {
                if (hasIV)
                {
                    _jobControllerService.GotPokemonIV(pkmn);
                }
                else
                {
                    _jobControllerService.GotPokemon(pkmn);
                }
            }
        }

        private void HandleFortPayload(string json, string username)
        {
            var fort = json.FromJson<PokemonFortProto>();
            if (fort == null)
            {
                // Failed to deserialize payload to PokemonFortProto
                _logger.LogError($"Failed to deserialize JSON payload to PokemonFortProto proto: {json}");
                return;
            }

            _jobControllerService.GotFort(fort, username);
        }

        private void HandleFortListPayload(string json, string username)
        {
            var forts = json.FromJson<List<PokemonFortProto>>();
            if (forts == null)
            {
                // Failed to deserialize payload to list of PokemonFortProto
                _logger.LogError($"Failed to deserialize JSON payload to list of PokemonFortProto proto: {json}");
                return;
            }

            foreach (var fort in forts)
            {
                _jobControllerService.GotFort(fort, username);
            }
        }

        private void HandlePlayerPayload(string json)
        {
            var data = json.FromJson<Dictionary<string, object>>();
            if (data == null)
            {
                // Failed to deserialize payload to dictionary
                _logger.LogError($"Failed to deserialize JSON payload to player data: {json}");
                return;
            }

            string username = Convert.ToString(data["username"]);
            ushort level = Convert.ToUInt16(Convert.ToString(data["level"]));
            ulong xp = Convert.ToUInt64(Convert.ToString(data["xp"]));

            _jobControllerService.GotPlayerData(username, level, xp);
        }
    }
}