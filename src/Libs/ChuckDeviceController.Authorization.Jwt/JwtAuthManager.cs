namespace ChuckDeviceController.Authorization.Jwt
{
    using System.IdentityModel.Tokens.Jwt;
    using System.Security.Claims;
    using System.Text;

    using Microsoft.Extensions.Logging;
    using Microsoft.IdentityModel.Tokens;

    using ChuckDeviceController.Configuration;
    using ChuckDeviceController.Protos;

    public class JwtAuthManager
    {
        #region Constants

        // TODO: Make JWT validity timespan configurable
        private const int JwtTokenValidity = 1; // minutes
        private const string DefaultGrpcServiceIdentifier = "Grpc";
        private const string DefaultInternalServiceIdentifier = "InternalService";
        private const string ClaimTypeNameRole = "role";

        #endregion

        #region Variables

        private static readonly ILogger<JwtAuthManager> _logger =
            new Logger<JwtAuthManager>(LoggerFactory.Create(x => x.AddConsole()));

        #endregion

        #region Singleton

        private static JwtAuthManager? _instance;
        public static JwtAuthManager Instance => _instance ??= new JwtAuthManager();

        #endregion

        #region Public Methods

        public JwtAuthResponse Generate(JwtAuthRequest request, JwtAuthConfig config)
        {
            var identifierRole = GetAuthRequestIdentifierRole(request);
            if (string.IsNullOrEmpty(identifierRole))
            {
                return new JwtAuthResponse
                {
                    Status = JwtAuthStatus.Error,
                };
            }

            var token = GenerateJwtToken(identifierRole, config);
            _logger.LogDebug($"Generated access token: {token}");
            return token;
        }

        public bool Validate(string token, JwtAuthConfig config)
        {
            if (string.IsNullOrEmpty(token))
                return false;

            return ValidateJwtToken(token, config);
        }

        #endregion

        #region Private Methods

        private static JwtAuthResponse GenerateJwtToken(string identifier, JwtAuthConfig config)
        {
            var id = Guid.NewGuid().ToString();
            var secret = Encoding.UTF8.GetBytes(config.Key);
            var tokenExpires = DateTime.UtcNow.AddMinutes(JwtTokenValidity);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Role, identifier),
                    new Claim(JwtRegisteredClaimNames.Jti, id),
                }),
                Expires = tokenExpires,
                Issuer = config.Issuer,
                Audience = config.Audience,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(secret),
                    SecurityAlgorithms.HmacSha512Signature
                )
            };
            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var jwtToken = tokenHandler.WriteToken(token);

            var response = new JwtAuthResponse
            {
                AccessToken = jwtToken,
                ExpiresIn = (uint)tokenExpires.Subtract(DateTime.UtcNow).TotalSeconds,
                Status = JwtAuthStatus.Ok,
            };
            return response;
        }

        private static bool ValidateJwtToken(string token, JwtAuthConfig config)
        {
            try
            {
                var key = Encoding.UTF8.GetBytes(config.Key);
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

                // Ensure the service identifier is set and is our constant internal service value
                var result = !string.IsNullOrEmpty(identifier) && identifier == DefaultInternalServiceIdentifier;
                return result;
            }
            catch //(Exception ex)
            {
                _logger.LogError($"Failed to validate the JWT token for gRPC service request.");
            }
            return false;
        }

        private static string? GetAuthRequestIdentifierRole(JwtAuthRequest request)
        {
            if (request.Identifier == DefaultGrpcServiceIdentifier)
            {
                return DefaultInternalServiceIdentifier;
            }
            return null;
        }

        #endregion
    }
}