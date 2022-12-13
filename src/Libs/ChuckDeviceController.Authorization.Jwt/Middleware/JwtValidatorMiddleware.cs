namespace ChuckDeviceController.Authorization.Jwt.Middleware
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Options;

    using ChuckDeviceController.Configuration;
    using ChuckDeviceController.Extensions.Http;

    public class JwtValidatorMiddleware
    {
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
            // Extract 'IgnoreJwtValidation' header value from request,
            // which indicates it's for the JwtAuth endpoint. If so, 
            // allow it to proceed otherwise validate JWT.
            //var ignoreValidationHeader = httpContext.Request.GetHeader(JwtStrings.IgnoreJwtValidationHeader);
            //if (ignoreValidationHeader == "1")
            //{
            //    await _next(httpContext);
            //    return;
            //}

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
                return;

            // Allow request to continue
            await _next(httpContext);
        }
    }
}