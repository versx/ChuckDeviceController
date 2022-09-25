namespace DeviceAuthPlugin
{
    using Microsoft.AspNetCore.Builder;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;

    using Configuration;
    using Extensions;

    using ChuckDeviceController.Common.Data;
    using ChuckDeviceController.Plugin;

    [PluginApiKey("CDC-328TVvD7o85TNbNhjLE0JysVMbOxjXKT")]
    public class DeviceAuthPlugin : IPlugin
    {
        private readonly IConfiguration _config;

        #region Metadata Properties

        public string Name => "DeviceAuthPlugin";

        public string Description => "";

        public string Author => "versx";

        public Version Version => new(1, 0, 0);

        #endregion

        #region Constructor

        public DeviceAuthPlugin(IConfigurationHost configHost)
        {
            // Load config
            _config = configHost.GetConfiguration();
        }

        #endregion

        #region ASP.NET Methods

        public void Configure(WebApplication appBuilder)
        {
            var tokenAuthConfig = new TokenAuthConfig();
            var ipAuthConfig = new IpAuthConfig();
            _config.GetSection("TokenAuth").Bind(tokenAuthConfig);
            _config.GetSection("IpAuth").Bind(ipAuthConfig);

            if (tokenAuthConfig.Enabled)
            {
                // Add device token auth middleware
                appBuilder.UseTokenAuth();
            }

            if (ipAuthConfig.Enabled)
            {
                // Add device IP address auth middleware
                appBuilder.UseIpAddressAuth();
            }
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<TokenAuthConfig>(_config.GetSection("TokenAuth"));
            services.Configure<IpAuthConfig>(_config.GetSection("IpAuth"));
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