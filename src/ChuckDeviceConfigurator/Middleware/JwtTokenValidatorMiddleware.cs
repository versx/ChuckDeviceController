namespace ChuckDeviceConfigurator.Middleware
{
    using System.IdentityModel.Tokens.Jwt;
    using System.Text;

    using Microsoft.Extensions.Options;
    using Microsoft.IdentityModel.Tokens;

    using ChuckDeviceConfigurator.Configuration;

    public class JwtTokenValidatorMiddleware
    {
        // TODO: Cache JWT tokens with expiration
        private const string DefaultContentType = "application/grpc";

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
                throw new ArgumentNullException($"JWT signing issuer key secret cannot be null!");
            }
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            if (httpContext.Request.ContentType == DefaultContentType)
            {
                // Only validate 'Authorization' header JWT if gRPC request.
                var token = GetAuthorizationHeader(httpContext.Request);
                var result = ValidateToken(httpContext, token);
                if (!result)
                {
                    return;
                }
            }

            await _next(httpContext);
        }

        private bool ValidateToken(HttpContext context, string token)
        {
            try
            {
                if (string.IsNullOrEmpty(token))
                    return false;

                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_jwtConfig.Key);
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
                var claim = jwtToken.Claims.FirstOrDefault(x => x.Type == "role");//ClaimTypes.Role);
                var identifier = claim?.Value;

                context.Items["Identifier"] = identifier;
                return true;

            }
            catch //(Exception ex)
            {
                _logger.LogError($"Failed to validate the JWT token for the gRPC service request to '{context.Request.Path}'.");
            }
            return false;
        }

        private static string GetAuthorizationHeader(HttpRequest request)
        {
            var token = request.Headers["Authorization"]
                .ToString()
                .Replace("Bearer ", null)
                .Replace("\"", null);
            return token;
        }
    }
}