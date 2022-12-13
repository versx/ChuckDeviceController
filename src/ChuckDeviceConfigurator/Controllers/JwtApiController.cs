namespace ChuckDeviceConfigurator.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Options;

    using ChuckDeviceController.Authorization.Jwt;
    using ChuckDeviceController.Authorization.Jwt.Models;
    using ChuckDeviceController.Configuration;

    [ApiController]
    public class JwtApiController : ControllerBase
    {
        private readonly JwtAuthConfig _config;

        public JwtApiController(IOptions<JwtAuthConfig> config)
        {
            _config = config.Value ?? new();
        }

        [HttpPost("api/jwt/generate")]
        [Produces("application/json")]
        public JwtResponse GenerateToken(string identifier)
        {
            //_logger.LogDebug($"Received JWT auth request for identifier '{request.Identifier}'");
            var response = JwtAuthManager.Instance.Generate(identifier, _config);
            return response;
        }

        [HttpPost("api/jwt/validate")]
        [Produces("application/json")]
        public bool ValidateToken(string token)
        {
            var valid = JwtAuthManager.Instance.Validate(token, _config);
            return valid;
        }
    }
}