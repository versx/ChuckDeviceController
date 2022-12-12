namespace ChuckDeviceController.Authorization.Jwt.Rpc.Interceptors
{
    using System.Collections.Concurrent;

    using Grpc.Core;
    using Grpc.Core.Interceptors;
    using Grpc.Net.Client;

    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    using ChuckDeviceController.Configuration;
    using ChuckDeviceController.Extensions;
    using ChuckDeviceController.Protos;

    // TODO: Pass property to determine whether JWT auth is enabled or not

    public class AuthHeadersInterceptor : Interceptor
    {
        #region Constants

        public const string AuthorizationHeader = "Authorization";
        public const string IgnoreJwtValidationHeader = "IgnoreJwtValidation";
        public const string DefaultGrpcServiceIdentifier = "Grpc";
        //public const string DefaultInternalServiceIdentifier = "InternalService";

        #endregion

        #region Variables

        private static readonly ILogger<AuthHeadersInterceptor> _logger =
            new Logger<AuthHeadersInterceptor>(LoggerFactory.Create(x => x.AddConsole()));
        protected static readonly ConcurrentDictionary<string, ulong> _jwtTokens = new();
        private static readonly object _lock = new();
        private static bool _isJwtAuthEnabled = true;

        private readonly GrpcEndpointsConfig _options;

        #endregion

        #region Constructor

        public AuthHeadersInterceptor(IOptions<GrpcEndpointsConfig> options)
        {
            _options = options.Value;
            if (string.IsNullOrEmpty(_options.Configurator))
            {
                throw new ArgumentNullException($"gRPC configurator server endpoint is not set but is required!", nameof(_options.Configurator));
            }
        }

        #endregion

        #region Impl Override Methods

        public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
            TRequest request,
            ClientInterceptorContext<TRequest, TResponse> context,
            AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
        {
            if (_isJwtAuthEnabled)
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
            if (response == null || response?.Status != JwtAuthStatus.Ok)
            {
                // Likely because JWT auth is disabled
                return null;
            }

            // Cache access token
            CacheAccessToken(response);

            // Create gRPC request headers with JWT token set
            // to 'Authorization' header.
            return CreateMetadata(response.AccessToken);
        }

        private async Task<JwtAuthResponse?> SendGenerateJwtTokenRequest()
        {
            try
            {
                using var channel = GrpcChannel.ForAddress(_options.Configurator);
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
            catch (RpcException rpcEx)
            {
                if (rpcEx.StatusCode == StatusCode.Unimplemented ||
                    rpcEx.Status.Detail == "Bad gRPC response. HTTP status code: 404")
                {
                    // gRPC JwtAuthServerService is not registered with configurator,
                    // set switch to false to skip generate JWT requests.
                    // NOTES: Needed otherwise sending requests to an endpoint that
                    // doesn't exist. Definitely not a good solution.
                    // TODO: Figure out solution to replace workaround.
                    _isJwtAuthEnabled = false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error: {ex}");
            }
            return null;
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
                    _jwtTokens.AddOrUpdate(response.AccessToken, expireTimestamp, (key, oldValue) => expireTimestamp);
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
                        if (!_jwtTokens.TryRemove(token, out var _))
                        {
                            _logger.LogError($"Failed to remove Jwt access token from cache...");
                        }
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