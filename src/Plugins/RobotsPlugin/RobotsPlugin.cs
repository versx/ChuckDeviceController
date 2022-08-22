namespace RobotsPlugin
{
    using Microsoft.AspNetCore.Builder;
    using Microsoft.Extensions.DependencyInjection;

    using Data.Contracts;
    using Data.Models;
    using Services;

    using ChuckDeviceController.Common.Data;
    using ChuckDeviceController.Plugin;

    // TODO: Implement web crawlers allow/deny on host application side

    [PluginPermissions(PluginPermissions.None)]
    [StaticFilesLocation(StaticFilesLocation.External)]
    public class RobotsPlugin : IPlugin
    {
        #region Variables

        private readonly IUiHost _uiHost;

        #endregion

        #region Constructor

        public RobotsPlugin(IUiHost uiHost)
        {
            _uiHost = uiHost;
        }

        #endregion

        #region Plugin Metadata Properties

        public string Name => "RobotsPlugin";

        public string Description => "";

        public string Author => "versx";

        public Version Version => new("1.0.0");

        #endregion

        #region ASP.NET WebApi Configure Callback Handlers

        public void Configure(WebApplication appBuilder)
        {
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<IRobots, Robots>();
            services.AddScoped<IRouteDataService, RouteDataService>();
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