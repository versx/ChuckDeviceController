namespace ChuckDeviceController.Services.Rpc.Interceptors
{
    using Grpc.Core;
    using Grpc.Core.Interceptors;
    using Grpc.Net.Client;

    using ChuckDeviceController.Extensions;
    using ChuckDeviceController.Protos;

    public class AuthHeadersInterceptor : Interceptor
    {
        private const string AuthorizationHeader = "Authorization";
        private const string IgnoreJwtValidationHeader = "IgnoreJwtValidation";
        private const string DefaultGrpcServiceIdentifier = "Grpc";
        private const string DefaultInternalServiceIdentifier = "InternalService";

        protected readonly Dictionary<string, ulong> _jwtTokens = new();
        private readonly string _grpcConfiguratorServerEndpoint;

        public AuthHeadersInterceptor(IConfiguration configuration)
        {
            var configuratorEndpoint = configuration.GetValue<string>("GrpcConfiguratorServer");
            if (string.IsNullOrEmpty(configuratorEndpoint))
            {
                throw new ArgumentNullException($"gRPC configurator server endpoint is not set but is required!", nameof(configuratorEndpoint));
            }
            _grpcConfiguratorServerEndpoint = configuratorEndpoint;
        }

        public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
            TRequest request,
            ClientInterceptorContext<TRequest, TResponse> context,
            AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
        {
            //var headers = CreateMetadata("failer");
            var headers = GetAuthorizationTokenAsync().Result;
            if (headers != null)
            {
                var callOptions = context.Options.WithHeaders(headers);
                context = new ClientInterceptorContext<TRequest, TResponse>(
                    context.Method,
                    context.Host,
                    callOptions
                );
            }
            return base.AsyncUnaryCall(request, context, continuation);
        }

        #region Private Methods

        private async Task<Metadata?> GetAuthorizationTokenAsync()
        {
            if (_jwtTokens.Any())
            {
                var token = GetCachedAccessToken();
                if (!string.IsNullOrEmpty(token))
                {
                    return CreateMetadata(token);
                }
            }

            using var channel = GrpcChannel.ForAddress(_grpcConfiguratorServerEndpoint);

            var client = new JwtAuth.JwtAuthClient(channel);
            var request = new JwtAuthRequest
            {
                Identifier = DefaultGrpcServiceIdentifier,
            };

            var headers = new Metadata
            {
                { IgnoreJwtValidationHeader, "1" },
            };
            var response = await client.GenerateAsync(request, headers);
            if (response == null || response.Status != JwtAuthStatus.Ok)
            {
                return null;
            }

            // Cache access token
            CacheAccessToken(response);

            // Cleanly shutdown gRPC client channel
            await channel.ShutdownAsync();

            return CreateMetadata(response.AccessToken);
        }

        private string GetCachedAccessToken()
        {
            // Get first token, check if expired - if so generate new one otherwise use existing
            var now = DateTime.UtcNow.ToTotalSeconds();
            // Attempt to retrieve the first JWT token that hasn't expired
            var (token, expires) = _jwtTokens.First(x => x.Value - now > 0 && x.Value > 0);
            return token;
        }

        private void CacheAccessToken(JwtAuthResponse response)
        {
            if (string.IsNullOrEmpty(response?.AccessToken) || response.ExpiresIn == 0)
                return;

            if (!_jwtTokens.ContainsKey(response.AccessToken))
            {
                var now = DateTime.UtcNow.ToTotalSeconds();
                _jwtTokens.Add(response.AccessToken, now + (ulong)response.ExpiresIn);
            }
        }

        private static Metadata CreateMetadata(string token)
        {
            var headers = new Metadata
            {
                { AuthorizationHeader, $"Bearer {token}" }
            };
            return headers;
        }

        #endregion
    }
}