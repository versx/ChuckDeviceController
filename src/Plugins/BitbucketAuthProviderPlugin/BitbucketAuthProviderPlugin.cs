namespace BitbucketAuthProviderPlugin
{
    using Microsoft.AspNetCore.Builder;
    using Microsoft.Extensions.DependencyInjection;

    using ChuckDeviceController.Common.Data;
    using ChuckDeviceController.Plugin;

    [PluginApiKey("CDC-328TVvD7o85TNbNhjLE0JysVMbOxjXKT")]
    public class BitbucketAuthProviderPlugin : IPlugin
    {
        #region Variables

        private readonly IConfiguration _config;

        #endregion

        #region Metadata Properties

        public string Name => "BitbucketAuthProviderPlugin";

        public string Description => "Adds Bitbucket.org authentication provider to possible 3rd party authentication providers.";

        public string Author => "versx";

        public Version Version => new(1, 0, 0);

        #endregion

        #region Constructor

        public BitbucketAuthProviderPlugin(IConfigurationHost configHost)
        {
            _config = configHost.GetConfiguration();
        }

        #endregion

        #region ASP.NET Methods

        public void Configure(WebApplication appBuilder)
        {
        }

        public void ConfigureServices(IServiceCollection services)
        {
            var authBuilder = services.AddAuthentication();

            if (_config.GetValue<bool>("Bitbucket:Enabled"))
            {
                var clientId = _config.GetValue<string>("Bitbucket:ClientId");
                var clientSecret = _config.GetValue<string>("Bitbucket:ClientSecret");
                authBuilder.AddBitbucket(options =>
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