namespace ChuckDeviceController.Services.Rpc
{
    using ChuckDeviceController.Protos;

    public class GrpcLevelingClient : IGrpcClient<Leveling.LevelingClient, TrainerInfoRequest, TrainerInfoResponse>
    {
        private readonly Leveling.LevelingClient _client;

        public GrpcLevelingClient(Leveling.LevelingClient client)
        {
            _client = client;
        }

        public async Task<TrainerInfoResponse?> SendAsync(TrainerInfoRequest payload)
        {
            try
            {
                var response = await _client.ReceivedTrainerInfoAsync(payload);
                return response;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SendAsync] Error: {ex.Message}");
            }
            return null;
        }
    }
}