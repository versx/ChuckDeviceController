namespace ChuckDeviceConfigurator.Middleware
{
    using System.IdentityModel.Tokens.Jwt;
    using System.Text;

    using Microsoft.Extensions.Options;
    using Microsoft.IdentityModel.Tokens;

    using ChuckDeviceController.Configuration;
    using ChuckDeviceController.Plugin.Helpers.Extensions;

    public class JwtTokenValidatorMiddleware
    {
        private const string DefaultContentType = "application/grpc";
        private const string ClaimTypeNameRole = "role";
        private const string IgnoreJwtValidationHeader = "IgnoreJwtValidation";

        private static readonly ILogger<JwtTokenValidatorMiddleware> _logger =
            new Logger<JwtTokenValidatorMiddleware>(LoggerFactory.Create(x => x.AddConsole()));
        private readonly RequestDelegate _next;
        private readonly JwtAuthConfig _jwtConfig;

        public JwtTokenValidatorMiddleware(RequestDelegate next, IOptions<JwtAuthConfig> options)
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
                // Check if request has 'IgnoreJwtValidation' header set,
                // which indicates it's for the JwtAuth endpoint. If
                // so, allow it to proceed otherwise validate JWT.
                var ignoreValidationHeader = httpContext.Request.GetHeader(IgnoreJwtValidationHeader);
                if (string.IsNullOrEmpty(ignoreValidationHeader))
                {
                    // Extract 'Authorization' header value from request
                    var token = httpContext.Request.GetAuthorizationHeader();
                    if (string.IsNullOrEmpty(token))
                    {
                        // No 'Authorization' header set but required
                        return;
                    }

                    // Validate JWT token from 'Authorization' header
                    var result = ValidateToken(httpContext, token);
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

        private bool ValidateToken(HttpContext context, string token)
        {
            if (string.IsNullOrEmpty(token))
                return false;

            try
            {
                var key = Encoding.UTF8.GetBytes(_jwtConfig.Key);
                var tokenHandler = new JwtSecurityTokenHandler();
                var tokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    // Set ClockSkew to zero so tokens expire exactly at their
                    // expiration time. (instead of 5 minutes later)
                    ClockSkew = TimeSpan.Zero,
                };

                var claimsPrincipal = tokenHandler.ValidateToken(
                    token,
                    tokenValidationParameters,
                    out SecurityToken validatedToken
                );

                var jwtToken = (JwtSecurityToken)validatedToken;
                var claim = jwtToken.Claims.FirstOrDefault(x => x.Type == ClaimTypeNameRole); //ClaimTypes.Role);
                var identifier = claim?.Value;

                context.Items[Strings.Identifier] = identifier;
                return true;
            }
            catch //(Exception ex)
            {
                _logger.LogError($"Failed to validate the JWT token for the gRPC service request to '{context.Request.Path}'.");
            }
            return false;
        }
    }
}