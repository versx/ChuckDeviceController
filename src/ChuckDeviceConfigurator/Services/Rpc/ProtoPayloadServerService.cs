namespace ChuckDeviceConfigurator.Services.Rpc
{
    using Grpc.Core;
    using POGOProtos.Rpc;

    using ChuckDeviceConfigurator.Services.Jobs;
    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Extensions;
    using ChuckDeviceController.Protos;

    public class ProtoPayloadServerService : Payload.PayloadBase
    {
        #region Variables

        private readonly ILogger<ProtoPayloadServerService> _logger;
        private readonly IJobControllerService _jobControllerService;

        #endregion

        #region Constructor

        public ProtoPayloadServerService(
            ILogger<ProtoPayloadServerService> logger,
            IJobControllerService jobControllerService)
        {
            _logger = logger;
            _jobControllerService = jobControllerService;
        }

        #endregion

        #region Event Handlers

        public override Task<PayloadResponse> ReceivedPayload(PayloadRequest request, ServerCallContext context)
        {
            //_logger.LogInformation($"Received {request.PayloadType} proto message");

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
                case PayloadType.PlayerInfo:
                    HandlePlayerInfoPayload(json);
                    break;
            }

            var response = new PayloadResponse
            {
                Status = PayloadStatus.Ok,
            };
            return Task.FromResult(response);
        }

        #endregion

        #region Payload Handlers

        private void HandlePokemonPayload(string json, bool hasIv)
        {
            var pokemon = json.FromJson<Pokemon>();
            if (pokemon == null)
            {
                // Failed to deserialize payload to Pokemon
                _logger.LogError($"Failed to deserialize JSON payload to Pokemon entity: {json}");
                return;
            }

            _jobControllerService.GotPokemon(pokemon, hasIv);
        }

        private void HandlePokemonListPayload(string json, bool hasIv)
        {
            var pokemon = json.FromJson<List<Pokemon>>();
            if (pokemon == null)
            {
                // Failed to deserialize payload to list of PokemonFortProto
                _logger.LogError($"Failed to deserialize JSON payload to list of PokemonFortProto proto: {json}");
                return;
            }

            _logger.LogInformation($"Received {pokemon.Count:N0} {PayloadType.Pokemon} proto messages");

            foreach (var pkmn in pokemon)
            {
                _jobControllerService.GotPokemon(pkmn, hasIv);
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

            _logger.LogInformation($"Received {forts.Count:N0} {PayloadType.Fort} proto messages");

            foreach (var fort in forts)
            {
                _jobControllerService.GotFort(fort, username);
            }
        }

        private void HandlePlayerInfoPayload(string json)
        {
            var data = json.FromJson<Dictionary<string, object>>();
            if (data == null)
            {
                // Failed to deserialize payload to dictionary
                _logger.LogError($"Failed to deserialize JSON payload to player data: {json}");
                return;
            }

            _logger.LogInformation($"Received {PayloadType.PlayerInfo} proto message");

            var username = Convert.ToString(data["username"]);
            var level = Convert.ToUInt16(Convert.ToString(data["level"]));
            var xp = Convert.ToUInt64(Convert.ToString(data["xp"]));

            _jobControllerService.GotPlayerInfo(username, level, xp);
        }

        #endregion
    }
}