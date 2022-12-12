namespace ChuckDeviceController.Services.Rpc
{
    using Grpc.Core;

    using ChuckDeviceController.Authorization.Jwt.Rpc.Interceptors;
    using ChuckDeviceController.Protos;

    public class GrpcJwtAuthClient : IGrpcClient<JwtAuth.JwtAuthClient, JwtAuthRequest, JwtAuthResponse>
    {
        private readonly ILogger<GrpcJwtAuthClient> _logger;
        private readonly JwtAuth.JwtAuthClient _client;

        public GrpcJwtAuthClient(
            ILogger<GrpcJwtAuthClient> logger,
            JwtAuth.JwtAuthClient client)
        {
            _logger = logger;
            _client = client;
        }

        public async Task<JwtAuthResponse?> SendAsync(JwtAuthRequest request)
        {
            try
            {
                var metadata = new Metadata
                {
                    { AuthHeadersInterceptor.IgnoreJwtValidationHeader, "1" },
                };
                var response = await _client.GenerateAsync(request, metadata);
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
