namespace TestPlugin;

using System.Collections.Generic;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using ChuckDeviceController.Data.Abstractions;
using ChuckDeviceController.Data.Common;
using ChuckDeviceController.Extensions.Http;
using ChuckDeviceController.Extensions.Json;
using ChuckDeviceController.Geometry.Models;
using ChuckDeviceController.Plugin;
using ChuckDeviceController.Plugin.EventBus;
using ChuckDeviceController.Plugin.EventBus.Events;
using ChuckDeviceController.Plugin.Services;

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
public class TestPlugin : IPlugin, IDatabaseEvents, IJobControllerServiceEvents, IUiEvents, ISettingsPropertyEvents
{
    #region Plugin Host Variables

    // Plugin host variables are interface contracts that are used
    // to interact with services the host application has registered
    // and is running. They can be initialized by the constructor
    // using dependency injection or by decorating the field with
    // the 'PluginBootstrapperService' attribute. The host application
    // will look for any fields or properties decorated with the
    // 'PluginBootstrapperService' and initialize them with the
    // related service class.

    // Used for logging messages to the host application from the plugin
    private readonly ILoggingHost _loggingHost;

    // Interacts with the job controller instance service to add new job
    // controllers.
    private readonly IJobControllerServiceHost _jobControllerHost;

    // Retrieve data from the database, READONLY.
    // 
    // When decorated with the 'PluginBootstrapperService' attribute, the
    // property will be initalized by the host's service implementation.
    [PluginBootstrapperService(typeof(IDatabaseHost))]
    private readonly IDatabaseHost _databaseHost = null!;

    // Translate text based on the set locale in the host application.
    private readonly ILocalizationHost _localeHost;

    // Expand your plugin implementation by adding user interface elements
    // and pages to the dashboard.
    // 
    // When decorated with the 'PluginBootstrapperService' attribute, the
    // property will be initalized by the host's service implementation.
    [PluginBootstrapperService(typeof(IUiHost))]
    private readonly IUiHost _uiHost = null!;

    // Manage files local to your plugin's folder using saving and loading
    // implementations.
    // 
    // When decorated with the 'PluginBootstrapperService' attribute, the
    // property will be initalized by the host's service implementation.
    [PluginBootstrapperService(typeof(IFileStorageHost))]
    private readonly IFileStorageHost _fileStorageHost = null!;

    [PluginBootstrapperService(typeof(IConfigurationHost))]
    private readonly IConfigurationHost _configurationHost = null!;

    private readonly IGeofenceServiceHost _geofenceServiceHost;

    private readonly IInstanceServiceHost _instanceServiceHost;

    private readonly IEventAggregatorHost _eventAggregatorHost;

    private readonly IAuthorizeHost _authHost;

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
    public Version Version => new(1, 0, 0);

    #endregion

    #region Plugin Host Properties

    /// <summary>
    ///     Gets or sets the UiHost host service implementation. This is
    ///     initialized separately from the '_uiHost' field that is decorated.
    /// </summary>
    /// <remarks>
    ///     When decorated with the 'PluginBootstrapperService' attribute, the
    ///     property will be initalized by the host's service implementation.
    /// </remarks>
    [PluginBootstrapperService(typeof(IUiHost))]
    public IUiHost UiHost { get; set; } = null!;

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
    /// <param name="localeHost">Localization host handler.</param>
    /// <param name="jobControllerServiceHost"></param>
    /// <param name="instanceServiceHost"></param>
    /// <param name="geofenceServiceHost"></param>
    /// <param name="eventAggregatorHost"></param>
    /// <param name="authHost"></param>
    public TestPlugin(
        ILoggingHost loggingHost,
        ILocalizationHost localeHost,
        IJobControllerServiceHost jobControllerServiceHost,
        IInstanceServiceHost instanceServiceHost,
        IGeofenceServiceHost geofenceServiceHost,
        IEventAggregatorHost eventAggregatorHost,
        IAuthorizeHost authHost)
    {
        _loggingHost = loggingHost;
        _localeHost = localeHost;
        _jobControllerHost = jobControllerServiceHost;
        _instanceServiceHost = instanceServiceHost;
        _geofenceServiceHost = geofenceServiceHost;
        _eventAggregatorHost = eventAggregatorHost;
        _authHost = authHost;

        //_appHost.Restart();
    }

    #endregion

    #region ASP.NET WebApi Configure Callback Handlers

    /// <summary>
    ///     Configures the application to set up middlewares, routing rules, etc.
    /// </summary>
    /// <param name="appBuilder">
    ///     Provides the mechanisms to configure an application's request pipeline.
    /// </param>
    public void Configure(WebApplication appBuilder)
    {
        _loggingHost.LogInformation($"Configure called");

        //var testService = appBuilder.Services.GetService<IPluginService>();

        // We can configure routing here using 'Minimal APIs' or using Mvc Controller classes
        // Minimal API's Reference: https://docs.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis?view=aspnetcore-6.0
        appBuilder.Map("/plugin/v1", app =>
        {
            app.Run(async (httpContext) =>
            {
                _loggingHost.LogInformation($"Plugin route called");
                await httpContext.Response.WriteAsync($"Hello from plugin {Name}");
            });
        });

        // Add additional endpoints to list on
        appBuilder.Urls.Add("http://*:1199"); // listen on all interfaces
        appBuilder.Urls.Add("http://+:1199"); // listen on all interfaces
        appBuilder.Urls.Add("http://0.0.0.0:1199"); // listen on all interfaces
        appBuilder.Urls.Add("http://localhost:1199");
        appBuilder.Urls.Add("http://127.0.0.1:1199");
        appBuilder.Urls.Add("http://10.0.0.2:1199");

        // Example routing using minimal APIs
        appBuilder.Map("example/{name}", async (httpContext) =>
        {
            Console.WriteLine($"Method: {httpContext.Request.Method}");
            var routeValues = httpContext.Request.RouteValues;
            var name = Convert.ToString(routeValues["name"]);
            Console.WriteLine($"Name: {name}");
            await httpContext.Response.WriteAsync(name!);
        });
        appBuilder.MapGet("example", () => "Hi :)");
        appBuilder.MapPost("example", async (httpContext) =>
        {
            var body = await httpContext.Request.ReadBodyAsStringAsync();
            _loggingHost.LogDebug($"Body: {body}");
            var coords = body?.FromJson<List<Coordinate>>();
            var response = string.Join(", ", coords ?? new());
            _loggingHost.LogDebug($"Coords: {response}");
            await httpContext.Response.WriteAsync(response);
        });
        //appBuilder.MapPut("example", async (httpContext) => { });
        //appBuilder.MapDelete("example", async (httpContext) => { });

        appBuilder.MapGet("example/hello/{name}", async (httpContext) =>
        {
            var method = httpContext.Request.Method;
            var path = httpContext.Request.Path;
            var queryValues = httpContext.Request.Query;
            // httpContext.Request.Form will throw an exception if 'Content-Type' is not 'application/application/www-x-form-urlencoded'
            //var formValues = httpContext.Request.Form;
            var routeValues = httpContext.Request.RouteValues;
            var body = httpContext.Request.Body;
            var userClaims = httpContext.User;
            await httpContext.Response.WriteAsync($"Hello, {routeValues["name"]}!");
        });
        appBuilder.MapGet("example/buenosdias/{name}", async (httpContext) =>
            await httpContext.Response.WriteAsync($"Buenos dias, {httpContext.Request.RouteValues["name"]}!"));
        appBuilder.MapGet("example/throw/{message?}", (httpContext) =>
            throw new Exception(Convert.ToString(httpContext.Request.RouteValues["message"]) ?? "Uh oh!"));
        appBuilder.MapGet("example/{greeting}/{name}", async (httpContext) =>
            await httpContext.Response.WriteAsync($"{httpContext.Request.RouteValues["greeting"]}, {httpContext.Request.RouteValues["name"]}!"));

        // NOTE: Uncommenting the below routing map will overwrite the default '/' routing path to the dashboard
        //appBuilder.MapGet("", async (httpContext) => await httpContext.Response.WriteAsync("Hello, World!"));


        // Register custom middlewares
        // Built in ASP.NET Core Middlewares: https://docs.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis?view=aspnetcore-6.0#aspnet-core-middleware
        //appBuilder.Use(async (httpContext, next) =>//(HttpContext httpContext, RequestDelegate req, Task next) =>
        //{
        //    // Action before next delegate
        //    await next.Invoke();
        //    // Action after called middleware
        //});

        // Use built in logger from dependency injection
        appBuilder.Logger.LogInformation($"Logging from the plugin '{Name}'");
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
        _loggingHost.LogInformation($"ConfigureServices called");

        //services.AddDbContext<TodoDbContext>(options => options.UseInMemoryDatabase("todo"), ServiceLifetime.Scoped);
    }

    /// <summary>
    ///     Provides an opportunity for plugins to configure Mvc Builder.
    /// </summary>
    /// <param name="mvcBuilder">
    ///     IMvcBuilder instance that can be configured.
    /// </param>
    public void ConfigureMvcBuilder(IMvcBuilder mvcBuilder)
    {
        _loggingHost.LogInformation($"ConfigureMvcBuilder called");

        // Configure localization for Views
        mvcBuilder
            .AddViewLocalization(
                LanguageViewLocationExpanderFormat.Suffix, options => 
                    options.ResourcesPath = "Resources")
            .AddDataAnnotationsLocalization();
    }

    #endregion

    #region Plugin Event Handlers

    /// <summary>
    ///     Called when the plugin is loaded and registered with the host application.
    ///     Loading UI elements here is the preferred location.
    /// </summary>
    public async void OnLoad()
    {
        _loggingHost.LogInformation($"{Name} v{Version} by {Author} initialized!");

        // Execute IFileStorageHost method tests
        TestFileStorageHost();

        // Execute IConfigurationHost method tests
        //TestConfigurationHost();

        // Add dashboard stats
        var stats = new List<IDashboardStatsItem>
        {
            new DashboardStatsItem("Test", "100", isHtml: false),
            new DashboardStatsItem("Test2", "<b><u>1,000</u></b>", isHtml: true),
            //new DashboardStatsItem("Test3", "<b>2,000</b>", isHtml: false),
        };
        await _uiHost.AddDashboardStatisticsAsync(stats);

        // Register new sidebar headers
        var pluginSidebarItems = new List<SidebarItem>
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
                dropdownItems: new List<SidebarItem>
                {
                    // Sidebar item #1
                    new("Page", "Test", "Index", displayIndex: 0, icon: "fa-solid fa-fw fa-vial"),
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
            new SidebarItem
            {
                Text = "Sep",
                DisplayIndex = 998,
                IsSeparator = true,
            },
        };
        await _uiHost.AddSidebarItemsAsync(pluginSidebarItems);

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

        var settingsTab = new SettingsTab
        {
            Id = "test",
            Text = "TestPlugin",
            Anchor = "test",
            DisplayIndex = 0,
        };
        await _uiHost.AddSettingsTabAsync(settingsTab);

        var settingsProperties = new List<SettingsProperty>
        {
            new("Enabled", "test-enabled", SettingsPropertyType.CheckBox, true),
            new("First Name", "FirstName", SettingsPropertyType.Text, "Jeremy", displayIndex: 1),
            new("TextAreaTest", "TextAreaTest", SettingsPropertyType.TextArea, "Testing", displayIndex: 2),
            new()
            {
                Text = "Year",
                Name = "Year",
                Value = 2022,
                Type = SettingsPropertyType.Number,
                DisplayIndex = 3,
            },
            new()
            {
                Text = "Geofences",
                Name = "Geofences",
                Value = new List<string> { "Paris", "London", "Sydney" },
                Type = SettingsPropertyType.Select,
                DisplayIndex = 0,
            },
        };
        await _uiHost.AddSettingsPropertiesAsync(settingsTab.Id, settingsProperties);

        TestLocaleHost();

        TestJobControllerServiceHost();

        TestDatabaseHost();

        //_eventAggregatorHost.Subscribe(new PluginObserver());
        _eventAggregatorHost.Publish(new PluginEvent("test message from plugin"));

        await TestAuthorizeHost();
    }

    /// <summary>
    ///     Called when the plugin has been reloaded by the host application.
    /// </summary>
    public void OnReload()
    {
        _loggingHost.LogInformation($"[{Name}] OnReload called");
        // TODO: Reload/re-register UI elements that might have been removed
    }

    /// <summary>
    ///     Called when the plugin has been stopped by the host application.
    /// </summary>
    public void OnStop() => _loggingHost.LogInformation($"[{Name}] OnStop called");

    /// <summary>
    ///     Called when the plugin has been removed by the host application.
    /// </summary>
    public void OnRemove() => _loggingHost.LogInformation($"[{Name}] Onremove called");

    /// <summary>
    ///     Called when the plugin's state has been
    ///     changed by the host application.
    /// </summary>
    /// <param name="state">Plugin's current state</param>
    public void OnStateChanged(PluginState state) =>
        _loggingHost.LogInformation($"[{Name}] Plugin state has changed to '{state}'");

    #endregion

    #region IDatabase Event Handlers

    public void OnStateChanged(DatabaseConnectionState state)
    {
        _loggingHost.LogInformation($"[{Name}] Plugin database connection state has changed: {state}");
    }

    public void OnEntityAdded<T>(T entity)
    {
        _loggingHost.LogInformation($"[{Name}] Plugin database entity has been added: {entity}");
    }

    public void OnEntityModified<T>(T oldEntity, T newEntity)
    {
        _loggingHost.LogInformation($"[{Name}] Plugin database entity has been modified: {oldEntity}->{newEntity}");
    }

    public void OnEntityDeleted<T>(T entity)
    {
        _loggingHost.LogInformation($"[{Name}] Plugin database entity has been deleted: {entity}");
    }

    #endregion

    #region ISettingsProperty Event Handlers

    public void OnSave(IReadOnlyDictionary<string, List<ISettingsProperty>> properties)
    {
        _loggingHost.LogInformation($"[{Name}] Plugin settings saved: {properties.Count:N0}");
    }

    #endregion

    #region Private Methods

    private void TestFileStorageHost()
    {
        var fileName = Name + ".deps.json";

        // Load dependencies config for plugin
        var fileData = _fileStorageHost.Load<DependenciesConfig>("", fileName);
        _loggingHost.LogInformation($"Loaded file data from '{fileName}': {fileData}");

        // Save dependencies config to new folder 'configs' in this plugins folder
        var fileSaveResult = _fileStorageHost.Save(fileData, "configs", fileName);
        _loggingHost.LogInformation($"Saved file data for '{fileName}': {fileSaveResult}");
    }

    private void TestConfigurationHost()
    {
        //var config = _configurationProviderHost.GetConfiguration<Dictionary<string, string>>(sectionName: "ConnectionStrings");
        var config = _configurationHost.GetConfiguration();
        var value = _configurationHost.GetValue<bool>("Enabled", sectionName: "Authentication:GitHub");
        _loggingHost.LogInformation($"Configuration: {config}, Value: {value}");

        var locale = _configurationHost.GetValue<string>("Locale");
        _loggingHost.LogInformation($"Configuration Locale: {locale}");
    }

    private async void TestDatabaseHost()
    {
        try
        {
            // Retrieve database entities 
            var device = await _databaseHost.FindAsync<IDevice, string>("SGV7SE");
            _loggingHost.LogInformation($"Device: {device?.Uuid}");

            var instance = await _databaseHost.FindAsync<IInstance, string>("TestInstance");
            _loggingHost.LogInformation($"Instance: {instance?.Name}");
            //var devices = await _databaseHost.GetListAsync<IDevice>();
            //_loggingHost.LogMessage($"Devices: {devices.Count}");

            //var device = await _databaseHost.Devices.GetByIdAsync("SGV7SE");
            //_loggingHost.LogMessage($"Device: {device}");

            //var accounts = await _databaseHost.Accounts.GetListAsync();
            //var accounts = await _databaseHost.GetListAsync<IAccount>();
            //_loggingHost.LogMessage($"Accounts: {accounts.Count}");
            //var pokestop = await _databaseHost.GetByIdAsync<IPokestop, string>("0192086043834f1c9c577a54a7890b32.16");
            //_loggingHost.LogMessage($"Pokestop: {pokestop.Name}");

            //var spawnpoints = await _databaseHost.GetAllAsync<ISpawnpoint>();
            //_loggingHost.LogDebug($"Spawnpoints: {spawnpoints.Count:N0}");

            //spawnpoints = await _databaseHost.FindAsync<ISpawnpoint, ulong>(
            //    spawnpoint => spawnpoint.DespawnSecond == null,
            //    spawnpoint => spawnpoint.Id,
            //    SortOrderDirection.Asc,
            //    50
            //);
            //_loggingHost.LogDebug($"Spawnpoints Exp: {spawnpoints?.Count:N0}");

            //var bannedAccounts = await _databaseHost.FindAsync<IAccount, string>(
            //    account => account.Failed == "suspended",
            //    account => account.Username,
            //    SortOrderDirection.Desc,
            //    10000
            //);
            //_loggingHost.LogInformation($"Banned Accounts: {bannedAccounts?.Count:N0}");
        }
        catch (Exception ex)
        {
            _loggingHost.LogError(ex);
        }
    }

    private void TestLocaleHost()
    {
        // Translate 1 to Bulbasaur
        var translated = _localeHost.GetPokemonName(1);
        _loggingHost.LogInformation($"Pokemon: {translated}");
    }

    private async void TestJobControllerServiceHost()
    {
        try
        {
            var customInstanceType = "test_controller";

            // Register custom job controller type TestInstanceController
            await _jobControllerHost.RegisterJobControllerAsync<TestInstanceController>(customInstanceType);

            // Create geofence entity
            //var geofence = CreateGeofence();
            //await _geofenceServiceHost.CreateGeofenceAsync(geofence);

            //var instance = CreateInstance(customInstanceType, new() { geofence.Name });
            //await _instanceServiceHost.CreateInstanceAsync(instance);

            //TestAssignDevice(instance.Name);
        }
        catch (Exception ex)
        {
            _loggingHost.LogError(ex);
        }
    }

    private async void TestAssignDevice(string instanceName)
    {
        // Assign device to new instance using custom job controller
        var uuid = "RH2SE"; //"SGV7SE";
        var device = await _databaseHost.FindAsync<IDevice, string>(uuid);
        if (device == null)
        {
            _loggingHost.LogError($"Failed to get device from database with UUID '{uuid}'");
            return;
        }

        if (device.InstanceName != instanceName)
        {
            await _jobControllerHost.AssignDeviceToJobControllerAsync(device, instanceName);
        }
    }

    private async Task TestAuthorizeHost()
    {
        var roleName = "TestRole";
        var result = await _authHost.RegisterRole(roleName, 3);
        _loggingHost.LogInformation($"Role Result: {result}");
    }

    private static Geofence CreateGeofence()
    {
        var geofence = new Geofence
        {
            Name = "TestGeofence",
            Type = GeofenceType.Circle,
            Data = new GeofenceData
            {
                Area = new List<Coordinate>
                {
                    new Coordinate(34.01, -117.01),
                    new Coordinate(34.02, -117.02),
                    new Coordinate(34.03, -117.03),
                    new Coordinate(34.04, -117.04),
                },
                ["test"] = "123", // <- Add custom properties
            },
        };
        return geofence;
    }

    private static Instance CreateInstance(string customInstanceType, List<string> geofences)
    {
        // Create instance
        var instance = new Instance
        {
            Name = "TestInstance",
            MinimumLevel = 30,
            MaximumLevel = 39,
            Geofences = geofences,
            Data = new InstanceData
            {
                CustomInstanceType = customInstanceType,
                ["test"] = "123", // <- Add custom properties
            },
        };
        return instance;
    }

    #endregion
}

// Mock {file}.deps.json configuration file model classes.
// 
// Since we are passing generic <T> type from host application to
// plugin (and vice versx) we do not need to register classes
// with host using 'PluginServiceAttribute' attribute decoration.
public class DependenciesConfig
{
    public RuntimeTarget RuntimeTarget { get; set; } = new();

    public Dictionary<string, object> CompilationOptions { get; set; } = new();

    public Dictionary<string, Dictionary<string, Dictionary<string, TargetDependencies>>> Targets { get; set; } = new();

    public Dictionary<string, Dictionary<string, object>> Libraries { get; set; } = new();
}

public class RuntimeTarget
{
    public string? Name { get; set; }

    public string? Signature { get; set; }
}

public class TargetDependencies
{
    public Dictionary<string, object> Dependencies { get; set; } = new();
}

public class Instance : IInstance
{
    public string Name { get; set; } = null!;

    public InstanceType Type => InstanceType.Custom;

    public ushort MinimumLevel { get; set; }

    public ushort MaximumLevel { get; set; }

    public List<string> Geofences { get; set; } = new();

    public InstanceData? Data { get; set; } = new();
}

public class Geofence : IGeofence
{
    public string Name { get; set; } = null!;

    public GeofenceType Type { get; set; }

    public GeofenceData? Data { get; set; } = new();
}