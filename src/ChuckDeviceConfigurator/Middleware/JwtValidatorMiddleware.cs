namespace ChuckDeviceConfigurator.Middleware
{
    using Microsoft.Extensions.Options;

    using ChuckDeviceConfigurator.Services.Rpc.Authorization;
    using ChuckDeviceController.Configuration;
    using ChuckDeviceController.Plugin.Helpers.Extensions;

    public class JwtValidatorMiddleware
    {
        private const string DefaultContentType = "application/grpc";
        private const string IgnoreJwtValidationHeader = "IgnoreJwtValidation";

        private readonly RequestDelegate _next;
        private readonly JwtAuthConfig _jwtConfig;

        public JwtValidatorMiddleware(RequestDelegate next, IOptions<JwtAuthConfig> options)
        {
            _next = next;
            _jwtConfig = options.Value;

            if (string.IsNullOrEmpty(_jwtConfig.Key))
            {
                throw new ArgumentNullException(nameof(_jwtConfig.Key), $"JWT signing issuer key secret cannot be null!");
            }
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            // Only validate 'Authorization' header JWT if gRPC request.
            if (httpContext.Request.ContentType == DefaultContentType)
            {
                // Extract 'IgnoreJwtValidation' header value from request,
                // which indicates it's for the JwtAuth endpoint. If so, 
                // allow it to proceed otherwise validate JWT.
                var ignoreValidationHeader = httpContext.Request.GetHeader(IgnoreJwtValidationHeader);
                if (string.IsNullOrEmpty(ignoreValidationHeader))
                {
                    // Extract 'Authorization' header value from request
                    var token = httpContext.Request.GetAuthorizationHeader();
                    if (string.IsNullOrEmpty(token))
                    {
                        // No 'Authorization' header set but required, ignore request
                        return;
                    }

                    // Validate JWT token from 'Authorization' header
                    var result = JwtAuthManager.Instance.Validate(token, _jwtConfig);
                    if (!result)
                    {
                        // Ignore request if validation failed
                        return;
                    }
                }
            }

            // Allow request to continue
            await _next(httpContext);
        }
    }
}