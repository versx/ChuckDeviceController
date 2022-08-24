namespace RobotsPlugin
{
    using Microsoft.AspNetCore.Builder;
    using Microsoft.Extensions.DependencyInjection;

    using Configuration;
    using Extensions;

    using ChuckDeviceController.Common.Data;
    using ChuckDeviceController.Plugin;

    [PluginPermissions(PluginPermissions.None)]
    [StaticFilesLocation(StaticFilesLocation.External)]
    public class RobotsPlugin : IPlugin
    {
        #region Variables

        private readonly IUiHost _uiHost;
        private readonly IConfiguration _config;

        #endregion

        #region Constructor

        public RobotsPlugin(IUiHost uiHost, IConfigurationHost configHost)
        {
            _uiHost = uiHost;
            _config = configHost.GetConfiguration();
        }

        #endregion

        #region Plugin Metadata Properties

        public string Name => "RobotsPlugin";

        public string Description => "Robot web crawlers management plugin allowing configuration" +
            " of routes and user agents that are allowed or disallowed.";

        public string Author => "versx";

        public Version Version => new("1.0.0");

        #endregion

        #region ASP.NET WebApi Configure Callback Handlers

        public void Configure(WebApplication appBuilder)
        {
            appBuilder.UseRobots();
        }

        public void ConfigureServices(IServiceCollection services)
        {
            //services.AddScoped<IRobots, Robots>();
            //services.AddScoped<IRouteDataService, RouteDataService>();
            services.Configure<WebCrawlerConfig>(_config.GetSection("WebCrawler"));
        }

        public void ConfigureMvcBuilder(IMvcBuilder mvcBuilder)
        {
        }

        #endregion

        #region Plugin Event Handlers

        public void OnLoad()
        {
            _uiHost.AddNavbarHeaderAsync(new NavbarHeader
            {
                Text = "Robots",
                ActionName = "Index",
                ControllerName = "Robot",
                Icon = "fa-solid fa-fw fa-robot",
                DisplayIndex = 999,
            });
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