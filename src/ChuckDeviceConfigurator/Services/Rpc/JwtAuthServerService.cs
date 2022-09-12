namespace ChuckDeviceConfigurator.Services.Rpc
{
    using Grpc.Core;
    using Microsoft.Extensions.Options;

    using ChuckDeviceController.Authorization.Jwt;
    using ChuckDeviceController.Configuration;
    using ChuckDeviceController.Protos;

    public class JwtAuthServerService : JwtAuth.JwtAuthBase
    {
        private readonly ILogger<JwtAuthServerService> _logger;
        private readonly JwtAuthConfig _jwtAuthConfig;

        public JwtAuthServerService(
            ILogger<JwtAuthServerService> logger,
            IOptions<JwtAuthConfig> options)
        {
            _logger = logger;
            _jwtAuthConfig = options.Value;
        }

        public override async Task<JwtAuthResponse> Generate(JwtAuthRequest request, ServerCallContext context)
        {
            //_logger.LogDebug($"Received JWT auth request for identifier '{request.Identifier}'");

            var response = JwtAuthManager.Instance.Generate(request, _jwtAuthConfig);
            if (response == null)
            {
                throw new RpcException(new Status(StatusCode.Unauthenticated, "Invalid identifier"));
            }
            return await Task.FromResult(response);
        }
    }
}