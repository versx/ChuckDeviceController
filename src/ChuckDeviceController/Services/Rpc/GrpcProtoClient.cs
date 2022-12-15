namespace ChuckDeviceController.Services.Rpc
{
    using ChuckDeviceController.Protos;

    public class GrpcProtoClient : IGrpcClient<Payload.PayloadClient, PayloadRequest, PayloadResponse>
    {
        private readonly ILogger<GrpcProtoClient> _logger;
        private readonly Payload.PayloadClient _client;

        public GrpcProtoClient(
            ILogger<GrpcProtoClient> logger,
            Payload.PayloadClient client)
        {
            _logger = logger;
            _client = client;
        }

        public async Task<PayloadResponse?> SendAsync(PayloadRequest payload)
        {
            try
            {
                var response = await _client.HandlePayloadAsync(payload);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error: {ex.InnerException?.Message ?? ex.Message}");
            }
            return null;
        }
    }
}
