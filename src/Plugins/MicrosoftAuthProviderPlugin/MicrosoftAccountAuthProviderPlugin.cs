namespace MicrosoftAccountAuthProviderPlugin
{
    using ChuckDeviceController.Common.Data;
    using ChuckDeviceController.Plugin;

    [PluginApiKey("CDC-328TVvD7o85TNbNhjLE0JysVMbOxjXKT")]
    public class MicrosoftAccountAuthProviderPlugin : IPlugin
    {
        #region Variables

        private readonly IConfiguration _config;

        #endregion

        #region Plugin Metadata Properties

        /// <summary>
        /// Gets the name of the plugin to use.
        /// </summary>
        public string Name => "MicrosoftAccountAuthProviderPlugin";

        /// <summary>
        /// Gets a brief description about the plugin explaining how it
        /// works and what it does.
        /// </summary>
        public string Description => "Adds Microsoft account authentication provider to possible 3rd party authentication providers.";

        /// <summary>
        /// Gets the name of the author/creator of the plugin.
        /// </summary>
        public string Author => "versx";

        /// <summary>
        /// Gets the current version of the plugin.
        /// </summary>
        public Version Version => new(1, 0, 0);

        #endregion

        #region Constructor

        public MicrosoftAccountAuthProviderPlugin(IConfigurationHost configHost)
        {
            _config = configHost.GetConfiguration();
        }

        #endregion

        #region ASP.NET WebApi Configure Callback Handlers

        public void Configure(WebApplication appBuilder)
        {
        }

        public void ConfigureServices(IServiceCollection services)
        {
            var authBuilder = services.AddAuthentication();

            if (_config.GetValue<bool>("Microsoft:Enabled"))
            {
                var clientId = _config.GetValue<string>("Microsoft:ClientId");
                var clientSecret = _config.GetValue<string>("Microsoft:ClientSecret");
                authBuilder.AddMicrosoftAccount(options =>
                {
                    options.ClientId = clientId!;
                    options.ClientSecret = clientSecret!;
                });
            }
        }

        public void ConfigureMvcBuilder(IMvcBuilder mvcBuilder)
        {
        }

        #endregion

        #region Plugin Event Handlers

        public void OnLoad()
        {
        }

        public void OnReload()
        {
        }

        public void OnStop()
        {
        }

        public void OnRemove()
        {
        }

        public void OnStateChanged(PluginState state)
        {
        }

        #endregion
    }
}