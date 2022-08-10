namespace TestPlugin
{
    using ChuckDeviceController.Common;
    using ChuckDeviceController.Common.Data.Contracts;
    using ChuckDeviceController.Plugins;

    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.DependencyInjection;

    using JobControllers;

    //http://127.0.0.1:8881/plugin/v1
    //http://127.0.0.1:8881/Test

    /// <summary>
    /// Example plugin demonstrating the capabilities of
    /// the plugin system and how it works.
    /// </summary>
    [PluginPermissions(PluginPermissions.ReadDatabase |
                       PluginPermissions.WriteDatabase |
                       PluginPermissions.DeleteDatabase |
                       PluginPermissions.AddControllers |
                       PluginPermissions.AddJobControllers)]
    public class TestPlugin : IPlugin
    {
        #region Plugin Host Variables

        // Plugin host variables are interface contracts that the plugin
        // to interact with services the host application is running.
        private readonly ILoggingHost _loggingHost;
        private readonly IJobControllerServiceHost _jobControllerHost;
        private readonly IDatabaseHost _databaseHost;
        private readonly ILocalizationHost _localeHost;
        private readonly IUiHost _uiHost;

        #endregion

        #region Plugin Metadata Properties

        /// <summary>
        /// Gets the name of the plugin to use.
        /// </summary>
        public string Name => "TestPlugin";

        /// <summary>
        /// Gets a brief description about the plugin explaining how it
        /// works and what it does.
        /// </summary>
        public string Description => "Demostrates the capabilities of the plugin system.";

        /// <summary>
        /// Gets the name of the author/creator of the plugin.
        /// </summary>
        public string Author => "versx";

        /// <summary>
        /// Gets the current version of the plugin.
        /// </summary>
        public Version Version => new("1.0.0.0");

        #endregion

        #region Constructor

        /// <summary>
        ///     Instantiates a new instance of <see cref="TestPlugin"/> with the host
        ///     application. It is important to only create one constructor for the
        ///     class that inherits the <see cref="IPlugin"/> interface contract,
        ///     otherwise the plugin will fail to load.
        /// 
        ///     This is so the host application knows which constructor to use when
        ///     it instantiates an instance with the host handler classes for each
        ///     parameter.
        /// </summary>
        /// <param name="loggingHost"></param>
        /// <param name="jobControllerHost"></param>
        /// <param name="databaseHost"></param>
        /// <param name="localeHost"></param>
        /// <param name="uiHost"></param>
        public TestPlugin(
            ILoggingHost loggingHost,
            IJobControllerServiceHost jobControllerHost,
            IDatabaseHost databaseHost,
            ILocalizationHost localeHost,
            IUiHost uiHost)
        {
            _loggingHost = loggingHost;
            _jobControllerHost = jobControllerHost;
            _databaseHost = databaseHost;
            _localeHost = localeHost;
            _uiHost = uiHost;

            //_appHost.Restart();
        }

        #endregion

        #region ASP.NET WebApi Event Handlers

        /// <summary>
        /// Configures the application to set up middlewares, routing rules, etc.
        /// </summary>
        /// <param name="appBuilder">
        /// Provides the mechanisms to configure an application's request pipeline.
        /// </param>
        public async void Configure(IApplicationBuilder appBuilder)
        {
            _loggingHost.LogMessage($"Configure called");

            // We can configure routing here or using Mvc Controller classes
            appBuilder.Map("/plugin/v1", app =>
            {
                app.Run(async (httpContext) =>
                {
                    _loggingHost.LogMessage($"Plugin route called");
                    await httpContext.Response.WriteAsync($"Hello from plugin {Name}");
                });
            });

            // Add dashboard stats
            var stats = new List<IDashboardStatsItem>
            {
                new DashboardStatsItem("Test", "100", false),
                new DashboardStatsItem("Test2", "<b><u>1,000</u></b>", true),
                //new DashboardStatsItem("Test3", "<b>2,000</b>", false),
            };
            await _uiHost.AddDashboardStatisticsAsync(stats);

            // Register new navbar headers
            var pluginNavbarHeaders = new List<NavbarHeader>
            {
                new(
                    text: "Test",
                    controllerName: "Test", // TestController class
                    actionName: "Index", // TestController.Index method
                    displayIndex: 1,
                    isDisabled: true
                ),
                new NavbarHeader
                {
                    Text = "TestNavDropdown",
                    DisplayIndex = 4,
                    IsDropdown = true,
                    DropDirection = NavbarHeaderDropDirection.Dropstart,
                    DropdownItems = new List<NavbarHeaderDropdownItem>
                    {
                        new("Item1", "Device"),
                        new("Hmm", isSeparator: true),
                        new("Item2", "Device", isDisabled: true),
                    },
                },
            };
            await _uiHost.AddNavbarHeadersAsync(pluginNavbarHeaders);

            var translated = _localeHost.GetPokemonName(1);
            _loggingHost.LogMessage($"Pokemon: {translated}");

            try
            {
                // Add/register TestInstanceController
                var coords = new List<ICoordinate>
                {
                    new Coordinate(34.01, -117.01),
                    new Coordinate(34.02, -117.02),
                    new Coordinate(34.03, -117.03),
                };
                var testController = new TestInstanceController("TestName", 30, 39, coords);
                await _jobControllerHost.AddJobControllerAsync(testController.Name, testController);

                // TODO: Show in Instances create/edit page
            }
            catch (Exception ex)
            {
                _loggingHost.LogException(ex);
            }

            try
            {
                // Retrieve database entities 
                var device = await _databaseHost.GetByIdAsync<IDevice, string>("SGV7SE");
                _loggingHost.LogMessage($"Device: {device?.Uuid}");
                //var devices = await _databaseHost.GetListAsync<IDevice>();
                //_loggingHost.LogMessage($"Devices: {devices.Count}");

                //var device = await _databaseHost.Devices.GetByIdAsync("SGV7SE");
                //_loggingHost.LogMessage($"Device: {device}");

                //var accounts = await _databaseHost.Accounts.GetListAsync();
                //var accounts = await _databaseHost.GetListAsync<IAccount>();
                //_loggingHost.LogMessage($"Accounts: {accounts.Count}");
                //var pokestop = await _databaseHost.GetByIdAsync<IPokestop, string>("0192086043834f1c9c577a54a7890b32.16");
                //_loggingHost.LogMessage($"Pokestop: {pokestop.Name}");
            }
            catch (Exception ex)
            {
                _loggingHost.LogException(ex);
            }
        }

        /// <summary>
        /// Register services into the IServiceCollection to use with Dependency Injection.
        /// This method is called first before the 'Configure(IApplicationBuilder)' method.
        /// </summary>
        /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
        public void ConfigureServices(IServiceCollection services)
        {
            _loggingHost.LogMessage($"ConfigureServices called");
            services.AddSingleton<IPluginService, TestPluginService>();

            //services.AddMvc();
            //services.AddControllersWithViews();
        }

        #endregion

        #region Plugin Event Handlers

        /// <summary>
        /// Called when the plugin is loaded and registered with the host application.
        /// </summary>
        public void OnLoad()
        {
            _loggingHost.LogMessage($"{Name} v{Version} by {Author} initialized!");
        }

        /// <summary>
        /// Called when the plugin has been reloaded by the host application.
        /// </summary>
        public void OnReload()
        {
            _loggingHost.LogMessage($"[{Name}] OnReload called");
        }

        /// <summary>
        /// Called when the plugin has been stopped by the host application.
        /// </summary>
        public void OnStop()
        {
            _loggingHost.LogMessage($"[{Name}] OnStop called");
        }

        /// <summary>
        /// Called when the plugin has been removed by the host application.
        /// </summary>
        public void OnRemove()
        {
            _loggingHost.LogMessage($"[{Name}] Onremove called");
        }

        /// <summary>
        /// Called when the plugin's state has been
        /// changed by the host application.
        /// </summary>
        /// <param name="state">Plugin's current state</param>
        /// <param name="isEnabled">Whether the plugin is
        /// currently enabled or disabled</param>
        public void OnStateChanged(PluginState state, bool isEnabled)
        {
            _loggingHost.LogMessage($"[{Name}] Plugin state has changed to '{state}'");
        }

        #endregion
    }

    public class Coordinate : ICoordinate
    {
        public double Latitude { get; set; }

        public double Longitude { get; set; }

        public Coordinate(double latitude, double longitude)
        {
            Latitude = latitude;
            Longitude = longitude;
        }
    }

    public class TestPluginService : IPluginService
    {
        public string Test => $"Testing";
    }
}