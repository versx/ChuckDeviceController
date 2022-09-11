namespace ChuckDeviceController.Services.Rpc.Interceptors
{
    using System.Net;

    using Grpc.Core;
    using Grpc.Core.Interceptors;

    using ChuckDeviceController.Net.Utilities;

    public class AuthHeadersInterceptor : Interceptor
    {
        // TODO: Make JWT endpoint url configurable
        private const string AuthorizationHeader = "Authorization";
        private const string CreateJwtTokenEndpoint = "http://127.0.0.1:8881/api/jwt/create?identifier=Grpc";

        public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
            TRequest request,
            ClientInterceptorContext<TRequest, TResponse> context,
            AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
        {
            //var accessToken = "failer";
            //var statusCode = HttpStatusCode.OK;
            var (statusCode, accessToken) = NetUtils.PostAsync(CreateJwtTokenEndpoint).Result;
            if (statusCode == HttpStatusCode.OK && !string.IsNullOrEmpty(accessToken))
            {
                var headers = new Metadata
                {
                    { AuthorizationHeader, $"Bearer {accessToken}" },
                };

                var callOptions = context.Options.WithHeaders(headers);
                context = new ClientInterceptorContext<TRequest, TResponse>(
                    context.Method,
                    context.Host,
                    callOptions
                );
            }

            return base.AsyncUnaryCall(request, context, continuation);
        }
    }
}