namespace ChuckDeviceController.Services.Rpc
{
    using ChuckDeviceController.Protos;

    public class GrpcProtoClient : IGrpcClient<Payload.PayloadClient, PayloadRequest, PayloadResponse>
    {
        private readonly Payload.PayloadClient _client;

        public GrpcProtoClient(Payload.PayloadClient client)
        {
            _client = client;
        }

        public async Task<PayloadResponse?> SendAsync(PayloadRequest payload)
        {
            try
            {
                var response = await _client.ReceivedPayloadAsync(payload);
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
