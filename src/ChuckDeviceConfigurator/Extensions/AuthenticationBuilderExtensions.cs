namespace ChuckDeviceConfigurator.Extensions
{
    using Microsoft.AspNetCore.Authentication;

    using ChuckDeviceConfigurator.Configuration;

    public static class AuthenticationBuilderExtensions
    {
        public static AuthenticationBuilder AddOpenAuthProviders(this AuthenticationBuilder auth, IConfiguration configuration)
        {
            var authConfig = configuration.GetSection("Authentication").Get<AuthenticationConfig>();
            if (authConfig == null)
            {
                // User does not have authentication options configured, allow
                // open instance instead of failing
                return auth;
            }

            // Check if GitHub auth is enabled, if so register it
            if (authConfig.GitHub.Enabled)
            {
                auth.AddGitHub(options =>
                {
                    options.ClientId = authConfig.GitHub.ClientId!;
                    options.ClientSecret = authConfig.GitHub.ClientSecret!;
                    //options.Scope("");
                });
            }

            // Check if Google auth is enabled, if so register it
            if (authConfig.Google.Enabled)
            {
                auth.AddGoogle(options =>
                {
                    options.ClientId = authConfig.Google.ClientId!;
                    options.ClientSecret = authConfig.Google.ClientSecret!;
                    options.Scope.Add("email");
                    options.Scope.Add("profile");
                    options.Scope.Add("openid");
                });
            }

            // Check if Discord auth is enabled, if so register it
            if (authConfig.Discord.Enabled)
            {
                auth.AddDiscord(options =>
                {
                    options.ClientId = authConfig.Discord.ClientId!;
                    options.ClientSecret = authConfig.Discord.ClientSecret!;
                    options.Scope.Add("email");
                    options.Scope.Add("guilds");
                    options.SaveTokens = true;
                });
            }

            return auth;
        }
    }
}