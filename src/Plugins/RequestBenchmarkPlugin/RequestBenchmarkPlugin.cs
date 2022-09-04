namespace RequestBenchmarkPlugin
{
    using Microsoft.AspNetCore.Builder;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;

    using ChuckDeviceController.Common.Data;
    using ChuckDeviceController.Plugin;

    using Data.Contexts;
    using Middleware;
    using Services;

    [StaticFilesLocation(StaticFilesLocation.Resources, StaticFilesLocation.External)]
    public class RequestBenchmarkPlugin : IPlugin
    {
        private const string DbName = "timing";

        private readonly IUiHost _uiHost;

        #region Plugin Metadata Properties

        public string Name => "RequestBenchmarkPlugin";

        public string Description => "Measures and benchmarks page request load times.";

        public string Author => "versx";

        public Version Version => new(1, 0, 0);

        #endregion

        #region Constructor

        public RequestBenchmarkPlugin(IUiHost uiHost)
        {
            _uiHost = uiHost;
        }

        #endregion

        #region ASP.NET WebApi Configure Callback Handlers

        public void Configure(WebApplication appBuilder)
        {
            appBuilder.UseMiddleware<RequestBenchmarkMiddleware>();
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<RequestTimesDbContext>(options => options.UseInMemoryDatabase(DbName), ServiceLifetime.Scoped);
            services.AddSingleton<IRequestBenchmarkService, RequestBenchmarkService>();
        }

        public void ConfigureMvcBuilder(IMvcBuilder mvcBuilder)
        {
        }

        #endregion

        #region Plugin Event Handlers

        public async void OnLoad()
        {
            var sidebarItem = new SidebarItem
            {
                Text = "Benchmarks",
                DisplayIndex = 1,
                Icon = "fa-solid fa-fw fa-clock",
                IsDropdown = true,
                DropdownItems = new List<SidebarItem>
                {
                    new SidebarItem
                    {
                        Text = "Requests",
                        ControllerName = "RequestTime",
                        ActionName = "Index",
                        Icon = "fa-solid fa-fw fa-globe",
                    },
                },
            };
            await _uiHost.AddSidebarItemAsync(sidebarItem);
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