namespace ChuckDeviceController.Services.Rpc.Interceptors
{
    using System.Net;

    using Grpc.Core;
    using Grpc.Core.Interceptors;

    using ChuckDeviceController.Common.Authorization;
    using ChuckDeviceController.Extensions;
    using ChuckDeviceController.Extensions.Json;
    using ChuckDeviceController.Net.Utilities;

    public class AuthHeadersInterceptor : Interceptor
    {
        // TODO: Make JWT endpoint url configurable
        // Possibly reuse gRPC service instead like originally planned
        private const string AuthorizationHeader = "Authorization";
        private const string CreateJwtTokenEndpoint = "http://127.0.0.1:8881/api/jwt/create?identifier=Grpc";

        protected readonly Dictionary<string, ulong> _jwtTokens = new();

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

        private async Task<Metadata?> GetAuthorizationTokenAsync()
        {
            if (_jwtTokens.Any())
            {
                // Get first token, check if expired - if so generate new one otherwise use existing
                var now = DateTime.UtcNow.ToTotalSeconds();
                // Attempt to retrieve the first JWT token that hasn't expired
                var (token, expires) = _jwtTokens.FirstOrDefault(x => x.Value - now > 0 && x.Value > 0);
                return CreateMetadata(token);
            }

            // No valid access tokens in cache, request new one
            var (statusCode, accessToken) = await NetUtils.PostAsync(CreateJwtTokenEndpoint);
            if (statusCode == HttpStatusCode.OK && !string.IsNullOrEmpty(accessToken))
            {
                // Convert access token response to JwtTokenResponse model
                var response = accessToken.FromJson<JwtTokenResponse>();
                if (response != null && response.Status == JwtAuthorizationStatus.OK)
                {
                    return CreateMetadata(response.AccessToken);
                }
            }

            return null;
        }

        private static Metadata CreateMetadata(string token)
        {
            var headers = new Metadata
            {
                { AuthorizationHeader, $"Bearer {token}" }
            };
            return headers;
        }
    }
}