namespace ChuckDeviceConfigurator.Services.Rpc.Authorization
{
    using System.IdentityModel.Tokens.Jwt;
    using System.Security.Claims;
    using System.Text;

    using Microsoft.IdentityModel.Tokens;

    using ChuckDeviceController.Configuration;
    using ChuckDeviceController.Protos;

    public class JwtAuthManager
    {
        private const int JwtTokenValidity = 30; // minutes
        private const string DefaultGrpcServiceIdentifier = "Grpc";
        private const string DefaultInternalServiceIdentifier = "InternalService";

        private static readonly ILogger<JwtAuthManager> _logger =
            new Logger<JwtAuthManager>(LoggerFactory.Create(x => x.AddConsole()));

        public static JwtAuthResponse Generate(JwtAuthRequest request, JwtAuthConfig config)
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
            //_logger.LogDebug($"Received access token: {token}");
            return token;
        }

        private static JwtAuthResponse GenerateJwtToken(string identifier, JwtAuthConfig config)
        {
            var secret = Encoding.UTF8.GetBytes(config.Key);
            var id = Guid.NewGuid().ToString();
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

        private static string GetAuthRequestIdentifierRole(JwtAuthRequest request)
        {
            if (request.Identifier == DefaultGrpcServiceIdentifier)
            {
                return DefaultInternalServiceIdentifier;
            }
            return null;
        }
    }
}