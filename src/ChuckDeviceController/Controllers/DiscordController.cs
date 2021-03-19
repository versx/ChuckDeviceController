namespace ChuckDeviceController.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;

    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;

    using Chuck.Data.Contexts;
    using Chuck.Data.Repositories;
    using ChuckDeviceController.Authentication.Models.Discord;
    using ChuckDeviceController.Extensions;

    [ApiController]
    public class DiscordController : ControllerBase
    {
        //private readonly DeviceControllerContext _context;
        private readonly ILogger<DiscordController> _logger;
        private readonly MetadataRepository _metadataRepository;

        private ulong _clientId;
        private string _clientSecret;
        private string _redirectUri;

        public static bool Enabled { get; private set; }

        public IReadOnlyList<ulong> UserIds { get; private set; }

        private const string AuthorizationEndpoint = "https://discordapp.com/api/oauth2/authorize";
        private const string TokenEndpoint = "https://discordapp.com/api/oauth2/token";
        private const string UserInformationEndpoint = "https://discordapp.com/api/users/@me";
        private const string UserGuildsInformationEndpoint = "https://discordapp.com/api/users/@me/guilds";

        public DiscordController(DeviceControllerContext context, ILogger<DiscordController> logger)
        {
            //_context = context;
            _logger = logger;
            _metadataRepository = new MetadataRepository(context);

            // Load values from database
            LoadSettings().ConfigureAwait(false).GetAwaiter().GetResult();
        }

        [NonAction]
        public async Task LoadSettings()
        {
            var enabled = await _metadataRepository.GetByIdAsync("DISCORD_ENABLED").ConfigureAwait(false);
            var clientId = await _metadataRepository.GetByIdAsync("DISCORD_CLIENT_ID").ConfigureAwait(false);
            var clientSecret = await _metadataRepository.GetByIdAsync("DISCORD_CLIENT_SECRET").ConfigureAwait(false);
            var redirectUri = await _metadataRepository.GetByIdAsync("DISCORD_REDIRECT_URI").ConfigureAwait(false);
            var userIds = await _metadataRepository.GetByIdAsync("DISCORD_USER_IDS").ConfigureAwait(false);
            _clientId = ulong.Parse(clientId?.Value);
            _clientSecret = clientSecret?.Value;
            _redirectUri = redirectUri?.Value;
            Enabled = bool.Parse(enabled?.Value);
            UserIds = userIds?.Value?.Split(',')
                                     .Select(ulong.Parse)
                                     .ToList();
        }

        #region Routes

        [HttpGet("/discord/login")]
        public IActionResult Login()
        {
            if (!IsEnabled())
            {
                return Redirect("/dashboard");
            }
            const string scope = "guilds%20identify%20email";
            var url = $"{AuthorizationEndpoint}?client_id={_clientId}&scope={scope}&response_type=code&redirect_uri={_redirectUri}";
            return Redirect(url);
        }

        [HttpGet("/discord/logout")]
        public IActionResult Logout()
        {
            if (!IsEnabled())
            {
                return Redirect("/dashboard");
            }
            HttpContext.Session.Clear();
            HttpContext.Session = null;
            // TODO: Fix destroying sessions
            return Redirect("/discord/login");
        }

        [HttpGet("/discord/callback")]
        public IActionResult Callback(string code)
        {
            if (!IsEnabled())
            {
                return Redirect("/dashboard");
            }
            if (string.IsNullOrEmpty(code))
            {
                // Error
                _logger.LogError("Authentication code is empty");
                return null;
            }

            var response = SendAuthorize(code);
            if (response == null)
            {
                // Error authorizing
                _logger.LogError("Failed to authenticate with Discord");
                return null;
            }

            // Successful
            var user = GetUser(response.TokenType, response.AccessToken);
            if (user == null)
            {
                // Failed to get user
                _logger.LogError("Failed to get user information");
                return null;
            }
            var guilds = GetUserGuilds(response.TokenType, response.AccessToken);
            if (guilds == null)
            {
                // Failed to get user guilds
                _logger.LogError($"Failed to get guilds for user {user.Username} ({user.Id})");
                return null;
            }
            // Validate user is in guild or user id matches
            var isValid = UserIds.Contains(ulong.Parse(user.Id));
            if (!isValid)
            {
                _logger.LogError($"Unauthorized user tried to authenticate {user.Username} ({user.Id}");
                return Redirect("/discord/login");
            }
            // User authenticated successfully
            _logger.LogInformation($"User {user.Username} ({user.Id}) authenticated successfully");
            HttpContext.Session.SetValue("is_valid", isValid);
            HttpContext.Session.SetValue("username", $"{user.Username}#{user.Discriminator}");
            HttpContext.Session.SetValue("guild_ids", guilds.Select(x => x.Id));
            // Check previous page saved if we should redirect to it or the home page
            var redirect = HttpContext.Session.GetValue<string>("last_redirect");
            HttpContext.Session.Remove("last_redirect");
            return Redirect(string.IsNullOrEmpty(redirect)
                ? "/dashboard"
                : redirect
            );
        }

        #endregion

        #region OAuth

        private DiscordAuthResponse SendAuthorize(string authorizationCode)
        {
            const string scope = "guilds%20identify%20email";
            using var wc = new WebClient();
            wc.Headers.Add(HttpRequestHeader.ContentType, "application/x-www-form-urlencoded");
            try
            {
                var result = wc.UploadValues(TokenEndpoint, new NameValueCollection
                {
                    { "client_id", _clientId.ToString() },
                    { "client_secret", _clientSecret },
                    { "grant_type", "authorization_code" },
                    { "code", authorizationCode },
                    { "redirect_uri", _redirectUri },
                    { "scope", scope },
                });
                var responseJson = Encoding.UTF8.GetString(result);
                var response = JsonSerializer.Deserialize<DiscordAuthResponse>(responseJson);
                return response;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static DiscordUserInfo GetUser(string tokenType, string token)
        {
            var response = SendRequest(UserInformationEndpoint, tokenType, token);
            var user = JsonSerializer.Deserialize<DiscordUserInfo>(response);
            return user;
        }

        private static List<DiscordGuildInfo> GetUserGuilds(string tokenType, string token)
        {
            var response = SendRequest(UserGuildsInformationEndpoint, tokenType, token);
            var guilds = JsonSerializer.Deserialize<List<DiscordGuildInfo>>(response);
            return guilds;
        }

        private static string SendRequest(string url, string tokenType, string token)
        {
            using var wc = new WebClient();
            wc.Proxy = null;
            wc.Headers[HttpRequestHeader.ContentType] = "application/json";
            wc.Headers[HttpRequestHeader.Authorization] = $"{tokenType} {token} ";
            return wc.DownloadString(url);
        }

        #endregion

        private bool IsEnabled()
        {
            return Enabled &&
                _clientId > 0 &&
                !string.IsNullOrEmpty(_clientSecret) &&
                !string.IsNullOrEmpty(_redirectUri);
        }
    }
}