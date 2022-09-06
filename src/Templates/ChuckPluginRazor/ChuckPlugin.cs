namespace ChuckDeviceControllerPluginRazor
{
    using Microsoft.AspNetCore.Builder;
    using Microsoft.Extensions.DependencyInjection;

    using ChuckDeviceController.Common.Data;
    using ChuckDeviceController.Plugin;
    using ChuckDeviceController.Plugin.Services;

    /// <summary>
    ///     Example plugin demonstrating the capabilities
    ///     of the plugin system and how it works.
    /// </summary>
    [
        // Specifies the permissions the plugin will require to the host application
        PluginPermissions(PluginPermissions.AddControllers),
        // Specifies where the 'wwwroot' folder will be if any are used or needed.
        // Possible options: embedded resources, local/external, or none.
        StaticFilesLocation(StaticFilesLocation.Resources, StaticFilesLocation.External),
    ]
    public class ChuckPluginRazor : IPlugin
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

        // Expand your plugin implementation by adding user interface elements
        // and pages to the dashboard.
        // 
        // When decorated with the 'PluginBootstrapperService' attribute, the
        // property will be initalized by the host's service implementation.
        [PluginBootstrapperService(typeof(IUiHost))]
        private readonly IUiHost _uiHost;

        #endregion

        #region Plugin Metadata Properties

        /// <summary>
        /// Gets the name of the plugin to use.
        /// </summary>
        public string Name => "ChuckPluginRazor";

        /// <summary>
        /// Gets a brief description about the plugin explaining how it
        /// works and what it does.
        /// </summary>
        public string Description => "Demostrates the capabilities of the plugin system using Razor pages architecture.";

        /// <summary>
        /// Gets the name of the author/creator of the plugin.
        /// </summary>
        public string Author => "versx";

        /// <summary>
        /// Gets the current version of the plugin.
        /// </summary>
        public Version Version => new(1, 0, 0);

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
        public ChuckPluginRazor(ILoggingHost loggingHost)
        {
            _loggingHost = loggingHost;
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

            // Register new sidebar headers
            var pluginSidebarItems = new List<SidebarItem>
            {
                new(
                    // Dropdown header text that is displayed in the sidebar
                    text: "Razor",
                    // Dropdown header display index in the sidebar
                    displayIndex: 1,
                    // Dropdown header Fontawesome icon
                    icon: "fa-solid fa-fw fa-highlighter",
                    // Yes we want this to be used as a dropdown and not just
                    // a single sidebar entry
                    isDropdown: true,
                    // List of children sidebar item
                    dropdownItems: new List<SidebarItem>
                    {
                        // Sidebar item #1
                        new("Test", "Tester", displayIndex: 0, icon: "fa-solid fa-fw fa-stapler"),
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
    }
}