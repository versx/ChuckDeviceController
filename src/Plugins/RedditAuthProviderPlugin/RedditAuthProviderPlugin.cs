namespace RedditAuthProviderPlugin
{
    using Microsoft.AspNetCore.Builder;
    using Microsoft.Extensions.DependencyInjection;

    using ChuckDeviceController.Common.Data;
    using ChuckDeviceController.Plugin;

    public class RedditAuthProviderPlugin : IPlugin
    {
        private readonly IConfiguration _config;

        #region Metadata Properties

        public string Name => "RedditAuthProviderPlugin";

        public string Description => "Adds Reddit.com authentication provider to possible 3rd party authentication providers.";

        public string Author => "versx";

        public Version Version => new(1, 0, 0);

        #endregion

        public RedditAuthProviderPlugin(IConfigurationHost configHost)
        {
            _config = configHost.GetConfiguration();
        }

        #region ASP.NET Methods

        public void Configure(WebApplication appBuilder)
        {
        }

        public void ConfigureServices(IServiceCollection services)
        {
            var authBuilder = services.AddAuthentication();

            if (_config.GetValue<bool>("Reddit:Enabled"))
            {
                var clientId = _config.GetValue<string>("Reddit:ClientId");
                var clientSecret = _config.GetValue<string>("Reddit:ClientSecret");
                authBuilder.AddReddit(options =>
                {
                    options.ClientId = clientId!;
                    options.ClientSecret = clientSecret!;
                    //options.Scope("");
                });
            }
        }

        public void ConfigureMvcBuilder(IMvcBuilder mvcBuilder)
        {
        }

        #endregion

        #region Plugin Methods

        public void OnLoad()
        {
        }

        public void OnReload()
        {
        }

        public void OnRemove()
        {
        }

        public void OnStop()
        {
        }

        public void OnStateChanged(PluginState state)
        {
        }

        #endregion
    }
}