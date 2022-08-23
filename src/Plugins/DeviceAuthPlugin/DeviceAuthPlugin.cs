namespace DeviceAuthPlugin
{
    using Microsoft.AspNetCore.Builder;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;

    using Configuration;
    using Extensions;

    using ChuckDeviceController.Common.Data;
    using ChuckDeviceController.Plugin;

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
            if (_config.GetValue<bool>("TokenAuth:Enabled"))
            {
                // Add device token auth middleware
                appBuilder.UseTokenAuth();
            }

            if (_config.GetValue<bool>("IpAuth:Enabled"))
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