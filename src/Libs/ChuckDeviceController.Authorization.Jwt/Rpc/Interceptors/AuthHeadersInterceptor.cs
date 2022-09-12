namespace ChuckDeviceController.Authorization.Jwt.Rpc.Interceptors
{
    using Grpc.Core;
    using Grpc.Core.Interceptors;
    using Grpc.Net.Client;

    using Microsoft.Extensions.Configuration;

    using ChuckDeviceController.Extensions;
    using ChuckDeviceController.Protos;

    public class AuthHeadersInterceptor : Interceptor
    {
        #region Constants

        private const string AuthorizationHeader = "Authorization";
        private const string IgnoreJwtValidationHeader = "IgnoreJwtValidation";
        private const string DefaultGrpcServiceIdentifier = "Grpc";
        private const string DefaultInternalServiceIdentifier = "InternalService";

        #endregion

        #region Variables

        protected static readonly Dictionary<string, ulong> _jwtTokens = new();
        private static readonly object _lock = new();
        private readonly string _grpcConfiguratorServerEndpoint;

        #endregion

        #region Constructor

        public AuthHeadersInterceptor(IConfiguration configuration)
        {
            var configuratorEndpoint = configuration.GetValue<string>("GrpcConfiguratorServer");
            if (string.IsNullOrEmpty(configuratorEndpoint))
            {
                throw new ArgumentNullException($"gRPC configurator server endpoint is not set but is required!", nameof(configuratorEndpoint));
            }
            _grpcConfiguratorServerEndpoint = configuratorEndpoint;
        }

        #endregion

        #region Impl Override Methods

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

        #endregion

        #region Token Methods

        private async Task<Metadata?> GetAuthorizationTokenAsync()
        {
            // Check if any JWT tokens have been cached
            lock (_lock)
            {
                if (_jwtTokens.Any())
                {
                    // Retrieve valid JWT access token from cache, otherwise generate new one
                    var token = GetCachedAccessToken();
                    // Ensure JWT token is not null
                    if (!string.IsNullOrEmpty(token))
                    {
                        // Create gRPC request headers with JWT token set
                        // to 'Authorization' header.
                        return CreateMetadata(token);
                    }
                }
            }

            // Send generate JWT token request
            var response = await SendGenerateJwtTokenRequest();
            // Ensure we received a response and that it was successful
            if (response == null || response.Status != JwtAuthStatus.Ok)
            {
                return null;
            }

            // Cache access token
            CacheAccessToken(response);

            // Create gRPC request headers with JWT token set
            // to 'Authorization' header.
            return CreateMetadata(response.AccessToken);
        }

        private async Task<JwtAuthResponse> SendGenerateJwtTokenRequest()
        {
            using var channel = GrpcChannel.ForAddress(_grpcConfiguratorServerEndpoint);
            var client = new JwtAuth.JwtAuthClient(channel);
            var request = new JwtAuthRequest
            {
                Identifier = DefaultGrpcServiceIdentifier,
            };

            var headers = CreateJwtMetadata();
            var response = await client.GenerateAsync(request, headers);

            // Cleanly shutdown gRPC client channel
            await channel.ShutdownAsync();

            return response;
        }

        #endregion

        #region Cache Methods

        private static string GetCachedAccessToken()
        {
            var now = DateTime.UtcNow.ToTotalSeconds();
            // Attempt to retrieve the first JWT token that hasn't expired
            var (token, expires) = _jwtTokens.FirstOrDefault(x => x.Value - now > 0 && x.Value > 0);
            if (string.IsNullOrEmpty(token) || now >= expires)
            {
                // If all tokens have expired, clean cache
                CleanAccessTokenCache();
            }
            return token;
        }

        private static void CacheAccessToken(JwtAuthResponse response)
        {
            lock (_lock)
            {
                if (_jwtTokens.ContainsKey(response.AccessToken))
                    return;

                if (!string.IsNullOrEmpty(response.AccessToken) && response.ExpiresIn > 0)
                {
                    var now = DateTime.UtcNow.ToTotalSeconds();
                    var expireTimestamp = now + response.ExpiresIn;
                    _jwtTokens.Add(response.AccessToken, expireTimestamp);
                }
            }
        }

        private static void CleanAccessTokenCache()
        {
            var now = DateTime.UtcNow.ToTotalSeconds();
            lock (_lock)
            {
                var keys = _jwtTokens.Keys.ToList();
                for (var i = 0; i < keys.Count; i++)
                {
                    var token = keys[i];
                    var expires = _jwtTokens[token];
                    if (now >= expires)
                    {
                        _jwtTokens.Remove(token);
                    }
                }
            }
        }

        #endregion

        #region Header Methods

        private static Metadata CreateMetadata(string token)
        {
            var headers = new Metadata
            {
                { AuthorizationHeader, $"Bearer {token}" }
            };
            return headers;
        }

        private static Metadata CreateJwtMetadata()
        {
            var headers = new Metadata
            {
                { IgnoreJwtValidationHeader, "1" },
            };
            return headers;
        }

        #endregion
    }
}