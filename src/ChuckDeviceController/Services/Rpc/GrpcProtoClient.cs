namespace ChuckDeviceController.Services.Rpc
{
    using ChuckDeviceController.Extensions.Json;
    using ChuckDeviceController.Protos;

    public interface IGrpcProtoClient
    {
        Task SendAsync<T>(T data, PayloadType payloadType, string? username = null, bool hasIV = false);
    }

    public class GrpcProtoClient : IGrpcProtoClient
    {
        private readonly Payload.PayloadClient _client;

        public GrpcProtoClient(Payload.PayloadClient client)
        {
            _client = client;
        }

        public async Task SendAsync<T>(T data, PayloadType payloadType, string? username = null, bool hasIV = false)
        {
            // Serialize entity and send to server to deserialize
            var json = data.ToJson();

            // Create gRPC payload request
            var request = new PayloadRequest
            {
                Payload = json,
                PayloadType = payloadType,
                Username = username,
                HasIV = hasIV,
            };

            // Handle the response of the request
            //var reply = await client.ReceivedPayloadAsync(request, deadline: DateTime.UtcNow.AddSeconds(5));
            var reply = await _client.ReceivedPayloadAsync(request);
            //Console.WriteLine($"Response: {reply?.Status}");
        }
    }
}
