namespace ChuckDeviceController.Services.Rpc
{
    using ChuckDeviceController.Protos;

    public class GrpcLevelingClient : IGrpcClient<Leveling.LevelingClient, TrainerInfoRequest, TrainerInfoResponse>
    {
        private readonly ILogger<GrpcLevelingClient> _logger;
        private readonly Leveling.LevelingClient _client;

        public GrpcLevelingClient(
            ILogger<GrpcLevelingClient> logger,
            Leveling.LevelingClient client)
        {
            _logger = logger;
            _client = client;
        }

        public async Task<TrainerInfoResponse?> SendAsync(TrainerInfoRequest payload)
        {
            try
            {
                var response = await _client.HandleTrainerInfoAsync(payload);
                return response;
            }
            catch (Exception)
            {
                //_logger.LogError($"Error: {ex.InnerException?.Message ?? ex.Message}");
            }
            return null;
        }
    }
}