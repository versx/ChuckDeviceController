namespace ChuckDeviceConfigurator.Controllers
{
    using System.IdentityModel.Tokens.Jwt;
    using System.Security.Claims;
    using System.Text;

    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.IdentityModel.Tokens;

    using ChuckDeviceController.Common.Authorization;

    [ApiController]
    [AllowAnonymous]
    [Route("api/jwt/")]
    public class JwtApiController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public JwtApiController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateToken(string identifier = "Grpc")
        {
            if (string.IsNullOrEmpty(identifier))
            {
                return new UnauthorizedResult();
            }

            var jwtSection = _configuration.GetSection("Jwt");
            var issuer = jwtSection.GetValue<string>("Issuer");
            var audience = jwtSection.GetValue<string>("Audience");
            var key = jwtSection.GetValue<string>("Key");
            var secret = Encoding.UTF8.GetBytes(key);
            var id = Guid.NewGuid().ToString();
            var tokenExpires = DateTime.UtcNow.AddMinutes(30);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Role, identifier),
                    new Claim(JwtRegisteredClaimNames.Jti, id),
                }),
                Expires = tokenExpires,
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(secret),
                    SecurityAlgorithms.HmacSha512Signature
                )
            };
            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var jwtToken = tokenHandler.WriteToken(token);

            var response = new JwtTokenResponse(
                accessToken: jwtToken,
                expiresIn: (uint)tokenExpires.Subtract(DateTime.UtcNow).TotalSeconds,
                status: JwtAuthorizationStatus.OK
            );
            var json = new JsonResult(response);
            return await Task.FromResult(json);
        }
    }
}