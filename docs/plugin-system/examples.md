# IDatabaseHost - Available Database Model Contracts

**Controller Types:**  
```cs
- IAccount
- IAssignment
- IAssignmentGroup
- IDevice
- IDeviceGroup
- IGeofence
- IInstance
- IIvList
- IWebhook
```

**Map Data Types:**  
```cs
- ICell
- IGym
- IGymDefender
- IGymTrainer
- IIncident
- IPokemon
- IPokestop
- ISpawnpoint
- IWeather
```


## Explanation
```cs
namespace TestPlugin
{
    using ChuckDeviceController.Common;
    using ChuckDeviceController.Common.Data;
    using ChuckDeviceController.Common.Data.Contracts;
    using ChuckDeviceController.Geometry.Models;
    using ChuckDeviceController.Geometry.Models.Contracts;
    using ChuckDeviceController.Plugins;

    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.DependencyInjection;

    using JobControllers;

    //http://127.0.0.1:8881/plugin/v1
    //http://127.0.0.1:8881/Test

    /// <summary>
    ///     Example plugin demonstrating the capabilities
    ///     of the plugin system and how it works.
    /// </summary>
    [
        // Specifies where the 'wwwroot' folder will be if any are used or needed.
        // Possible options: embedded resources, local/external, or none.
        StaticFilesLocation(StaticFilesLocation.Resources, StaticFilesLocation.External),
        // Specify the plugin API key to authorize with the host application.
        PluginApiKey("CDC-328TVvD7o85TNbNhjLE0JysVMbOxjXKT"),
    ]
    public class TestPlugin : IPlugin, IDatabaseEvents, IJobControllerServiceEvents, IUiEvents
    {
        #region Plugin Host Variables

        // Plugin host variables are interface contracts that are used
        // to interact with services the host application has registered
        // and is running.

        // Used for logging messages to the host application from the plugin
        private readonly ILoggingHost _loggingHost;
        // Interacts with the job controller instance service to add new job
        // controllers.
        private readonly IJobControllerServiceHost _jobControllerHost;
        // Retrieve data from the database, READONLY.
        private readonly IDatabaseHost _databaseHost;
        // Translate text based on the set locale in the host application.
        private readonly ILocalizationHost _localeHost;
        // Expand your plugin implementation by adding user interface elements
        // and pages to the dashboard.
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
        ///     Instantiates a new instance of <see cref="IPlugin"/> with the host
        ///     application. It is important to only create one constructor for the
        ///     class that inherits the <see cref="IPlugin"/> interface contract.
        ///     Failure to do so will prevent the plugin from loading.
        /// 
        ///     This is so the host application knows which constructor to use
        ///     when it instantiates an instance with the host handlers for each 
        ///     parameter, essentially dependency injection.
        /// </summary>
        /// <param name="loggingHost">Logging host handler.</param>
        /// <param name="jobControllerHost">Job controller host handler.</param>
        /// <param name="databaseHost">Database host handler.</param>
        /// <param name="localeHost">Localization host handler.</param>
        /// <param name="uiHost">User interface host handler.</param>
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
        ///     Configures the application to set up middlewares, routing rules, etc.
        /// </summary>
        /// <param name="appBuilder">
        ///     Provides the mechanisms to configure an application's request pipeline.
        /// </param>
        public void Configure(IApplicationBuilder appBuilder)
        {
            _loggingHost.LogMessage($"Configure called");

            var testService = appBuilder.ApplicationServices.GetService<IPluginService>();

            // We can configure routing here or using Mvc Controller classes
            appBuilder.Map("/plugin/v1", app =>
            {
                app.Run(async (httpContext) =>
                {
                    _loggingHost.LogMessage($"Plugin route called");
                    await httpContext.Response.WriteAsync($"Hello from plugin {Name}");
                });
            });
        }

        /// <summary>
        ///     Register services into the IServiceCollection to use with Dependency Injection.
        ///     This method is called first before the 'Configure(IApplicationBuilder)' method.
        /// 
        ///     Register service(s) with Mvc using dependency injection. Services can be passed to
        ///     other services via the constructor. Depending on the service, you can register the
        ///     service lifetime as 'Singleton', 'Transient', or 'Scoped'.
        /// 
        ///
        ///     - Transient objects are always different.The transient OperationId value is different in the IndexModel and in the middleware.
        ///     - Scoped objects are the same for a given request but differ across each new request.
        ///     - Singleton objects are the same for every request.
        ///     
        ///     More details: https://docs.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-6.0#constructor-injection-behavior
        /// </summary>
        /// <param name="services">
        ///     Specifies the contract for a collection of service descriptors.
        /// </param>
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
        ///     Called when the plugin is loaded and registered with the host application.
        /// </summary>
        public async void OnLoad()
        {
            _loggingHost.LogMessage($"{Name} v{Version} by {Author} initialized!");

            // Add dashboard stats
            var stats = new List<IDashboardStatsItem>
            {
                new DashboardStatsItem("Test", "100", isHtml: false),
                new DashboardStatsItem("Test2", "<b><u>1,000</u></b>", isHtml: true),
                //new DashboardStatsItem("Test3", "<b>2,000</b>", isHtml: false),
            };
            await _uiHost.AddDashboardStatisticsAsync(stats);

            // Register new navbar headers
            var pluginNavbarHeaders = new List<NavbarHeader>
            {
                new(
                    // Dropdown header text that is displayed in the sidebar
                    text: "Test",
                    // Dropdown header display index in the sidebar
                    displayIndex: 0,
                    // Dropdown header Fontawesome icon
                    icon: "fa-solid fa-fw fa-microscope",
                    // Yes we want this to be used as a dropdown and not just
                    // a single sidebar entry
                    isDropdown: true,
                    // List of children sidebar item
                    dropdownItems: new List<NavbarHeaderDropdownItem>
                    {
                        // Sidebar item #1
                        new("Page", "Test", displayIndex: 0, icon: "fa-solid fa-fw fa-vial"),
                        // Sidebar item #2
                        new(
                            // Text that is displayed in the sidebar
                            "Details",
                            // 'Test' is the MVC view controller 'TestController.cs'
                            "Test",
                            // 'Details' is the controller action (method name) that is executed when the navbar header is clicked
                            "Details",
                            // Display index in the sidebar
                            displayIndex: 1,
                            // Fontawesome icon to include (optional)
                            icon: "fa-solid fa-fw fa-hammer",
                            // Whether the sidebar item is disabled and not clickable
                            isDisabled: true
                        ),
                    }
                ),
                new NavbarHeader
                {
                    Text = "Devices", //"TestNavDropdown",
                    DisplayIndex = 4,
                    Icon = "fa-solid fa-fw fa-microscope",
                    IsDropdown = true,
                    DropdownItems = new List<NavbarHeaderDropdownItem>
                    {
                        new("Item1", "Device", displayIndex: 0, icon: "fa-solid fa-fw fa-mobile-alt"),
                        new("Hmm", isSeparator: true, displayIndex: 3),
                        new("Item2", "Instance", isDisabled: true, displayIndex: 999, icon: "fa-solid fa-fw fa-cubes-stacked"),
                    },
                },
            };
            await _uiHost.AddNavbarHeadersAsync(pluginNavbarHeaders);

            // Add/register dashboard tiles
            var pluginTile = new DashboardTile
            (
                text: "Test",
                value: "5,000",
                icon: "fa-solid fa-fw fa-hammer",
                controllerName: "Test",
                actionName: "Index"
            );
            await _uiHost.AddDashboardTileAsync(pluginTile);

            // Translate 1 to Bulbasaur
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
        ///     Called when the plugin has been reloaded by the host application.
        /// </summary>
        public void OnReload()
        {
            _loggingHost.LogMessage($"[{Name}] OnReload called");
            // TODO: Reload/re-register UI elements that might have been removed
        }

        /// <summary>
        ///     Called when the plugin has been stopped by the host application.
        /// </summary>
        public void OnStop()
        {
            _loggingHost.LogMessage($"[{Name}] OnStop called");
            // TODO: Unregister all UI items (or leave that up to the host app)?
        }

        /// <summary>
        ///     Called when the plugin has been removed by the host application.
        /// </summary>
        public void OnRemove()
        {
            _loggingHost.LogMessage($"[{Name}] Onremove called");
        }

        /// <summary>
        ///     Called when the plugin's state has been
        ///     changed by the host application.
        /// </summary>
        /// <param name="state">Plugin's current state</param>
        public void OnStateChanged(PluginState state)
        {
            _loggingHost.LogMessage($"[{Name}] Plugin state has changed to '{state}'");
        }

        #endregion

        #region IDatabase Event Handlers

        public void OnStateChanged(DatabaseConnectionState state)
        {
            _loggingHost.LogMessage($"[{Name}] Plugin database connection state has changed: {state}");
        }

        public void OnEntityAdded<T>(T entity)
        {
            _loggingHost.LogMessage($"[{Name}] Plugin database entity has been added: {entity}");
        }

        public void OnEntityModified<T>(T oldEntity, T newEntity)
        {
            _loggingHost.LogMessage($"[{Name}] Plugin database entity has been modified: {oldEntity}->{newEntity}");
        }

        public void OnEntityDeleted<T>(T entity)
        {
            _loggingHost.LogMessage($"[{Name}] Plugin database entity has been deleted: {entity}");
        }

        #endregion
    }

    /// <summary>
    ///     TODO: Test service class for dependency injection. **DO NOT USE**:
    ///     There is no implementation. Need to add support for host application
    ///     referencing plugin shared assemblies between host and plugin for 
    ///     service registration to work properly.
    /// </summary>
    public class TestPluginService : IPluginService
    {
        public string Test => $"Testing";
    }
}
```
