namespace ChuckDeviceController.Services.Rpc
{
    using ChuckDeviceController.Protos;

    public interface IGrpcLevelingClient
    {
        Task<TrainerInfoResponse?> SendAsync(string username);
    }

    public class GrpcLevelingClient : IGrpcLevelingClient
    {
        private readonly Leveling.LevelingClient _client;

        public GrpcLevelingClient(Leveling.LevelingClient client)
        {
            _client = client;
        }

        public async Task<TrainerInfoResponse?> SendAsync(string username)
        {
            // Create gRPC payload request
            var request = new TrainerInfoRequest
            {
                Username = username,
            };

            // Handle the response of the request
            var response = await _client.ReceivedTrainerInfoAsync(request);
            return response;
        }
    }
}