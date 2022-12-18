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

    [PluginApiKey("CDC-328TVvD7o85TNbNhjLE0JysVMbOxjXKT")]
    [StaticFilesLocation(StaticFilesLocation.Resources, StaticFilesLocation.External)]
    public class RequestBenchmarkPlugin : IPlugin
    {
        //public const string RequestBenchmarkRoleName = "RequestBenchmark";
        //public const string RequestBenchmarkRole = $"{nameof(Roles.SuperAdmin)},{nameof(Roles.Admin)},{RequestBenchmarkRoleName}";

        private const string DbName = "timings";

        private readonly IUiHost _uiHost;
        private readonly IConfiguration _config;
        private readonly IAuthorizeHost _authHost;

        #region Plugin Metadata Properties

        public string Name => "RequestBenchmarkPlugin";

        public string Description => "Measures and benchmarks page request load times.";

        public string Author => "versx";

        public Version Version => new(1, 0, 0);

        #endregion

        #region Constructor

        public RequestBenchmarkPlugin(IUiHost uiHost, IConfigurationHost configHost, IAuthorizeHost authHost)
        {
            _uiHost = uiHost;
            _config = configHost.GetConfiguration();
            _authHost = authHost;
        }

        #endregion

        #region ASP.NET WebApi Configure Callback Handlers

        public void Configure(WebApplication appBuilder)
        {
            appBuilder.UseMiddleware<RequestBenchmarkMiddleware>();
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<RequestBenchmarkConfig>(_config);
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

            //await _authHost.RegisterRole(RequestBenchmarkRoleName);
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