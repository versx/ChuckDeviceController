namespace ChuckDeviceCommunicator.Services.Rpc
{
    using Grpc.Net.Client;

    using ChuckDeviceController.Protos;

    public class GrpcClientService : IGrpcClientService
    {
        private readonly string _grpcControllerServerEndpoint;

        public GrpcClientService(IConfiguration configuration)
        {
            var controllerEndpoint = configuration.GetValue<string>("ControllerServerEndpoint");
            if (string.IsNullOrEmpty(controllerEndpoint))
            {
                throw new ArgumentNullException($"gRPC controller server endpoint is not set but is required!", nameof(controllerEndpoint));
            }
            _grpcControllerServerEndpoint = controllerEndpoint;
        }

        public async Task<WebhookEndpointResponse> GetWebhookEndpointsAsync()
        {
            // Create gRPC channel for receiving gRPC server address
            using var channel = GrpcChannel.ForAddress(_grpcControllerServerEndpoint);

            // Create new gRPC client for gRPC channel for address
            var client = new WebhookEndpoint.WebhookEndpointClient(channel);

            // Create gRPC payload request
            var request = new WebhookEndpointRequest();

            // Handle the response of the request
            var response = await client.ReceivedWebhookEndpointAsync(request);
            return response;
        }
    }
}
