namespace ChuckPlugin
{
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;

    using ChuckDeviceController.Common.Data;
    using ChuckDeviceController.Plugin;
    using ChuckDeviceController.Plugin.Services;

    /// <summary>
    ///     Example plugin demonstrating the capabilities
    ///     of the plugin system and how it works.
    /// </summary>
    [
        // Specifies the permissions the plugin will require to the host application
        PluginPermissions(PluginPermissions.None),
        // Specifies where the 'wwwroot' folder will be if any are used or needed.
        // Possible options: embedded resources, local/external, or none.
        StaticFilesLocation(views: StaticFilesLocation.None, webRoot: StaticFilesLocation.None),
    ]
    public class ChuckPlugin : IPlugin, IDatabaseEvents, IJobControllerServiceEvents, IUiEvents, ISettingsPropertyEvents
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
        //private readonly IJobControllerServiceHost _jobControllerHost;

        // Retrieve data from the database, READONLY.
        // 
        // When decorated with the 'PluginBootstrapperService' attribute, the
        // property will be initalized by the host's service implementation.
        [PluginBootstrapperService(typeof(IDatabaseHost))]
        private readonly IDatabaseHost _databaseHost;

        // Translate text based on the set locale in the host application.
        private readonly ILocalizationHost _localeHost;

        // Expand your plugin implementation by adding user interface elements
        // and pages to the dashboard.
        // 
        // When decorated with the 'PluginBootstrapperService' attribute, the
        // property will be initalized by the host's service implementation.
        [PluginBootstrapperService(typeof(IUiHost))]
        private readonly IUiHost _uiHost;

        // Manage files local to your plugin's folder using saving and loading
        // implementations.
        // 
        // When decorated with the 'PluginBootstrapperService' attribute, the
        // property will be initalized by the host's service implementation.
        [PluginBootstrapperService(typeof(IFileStorageHost))]
        private readonly IFileStorageHost _fileStorageHost;

        [PluginBootstrapperService(typeof(IConfigurationHost))]
        private readonly IConfigurationHost _configurationHost;

        #endregion

        #region Plugin Metadata Properties

        /// <summary>
        /// Gets the name of the plugin to use.
        /// </summary>
        public string Name => "ChuckDeviceControllerPluginMvc1";

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
        public IUiHost UiHost { get; set; }

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
        public ChuckPlugin(
            ILoggingHost loggingHost,
            ILocalizationHost localeHost)
        {
            _loggingHost = loggingHost;
            _localeHost = localeHost;
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
            _loggingHost.LogMessage($"Configure called");

            // Add additional endpoints to list on
            appBuilder.Urls.Add("http://127.0.0.1:1199");

            // Example routing using minimal APIs
            // We can configure routing here using 'Minimal APIs' or using Mvc Controller classes
            // Minimal API's Reference: https://docs.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis?view=aspnetcore-6.0
            appBuilder.MapDelete("example", async (httpContext) => { });

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

            // Use built in logger
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
            _loggingHost.LogMessage($"ConfigureServices called");
            // NOTE: Not used in non-ASP.NET projects
        }

        /// <summary>
        ///     Provides an opportunity for plugins to configure Mvc Builder.
        /// </summary>
        /// <param name="mvcBuilder">
        ///     IMvcBuilder instance that can be configured.
        /// </param>
        public void ConfigureMvcBuilder(IMvcBuilder mvcBuilder)
        {
            _loggingHost.LogMessage($"ConfigureMvcBuilder called");
            // NOTE: Not used in non-ASP.NET projects
        }

        #endregion

        #region Plugin Event Handlers

        /// <summary>
        ///     Called when the plugin is loaded and registered with the host application.
        ///     Loading UI elements here is the preferred location.
        /// </summary>
        public void OnLoad()
        {
            _loggingHost.LogMessage($"{Name} v{Version} by {Author} initialized!");
        }

        /// <summary>
        ///     Called when the plugin has been reloaded by the host application.
        /// </summary>
        public void OnReload()
        {
            _loggingHost.LogMessage($"[{Name}] OnReload called");
        }

        /// <summary>
        ///     Called when the plugin has been stopped by the host application.
        /// </summary>
        public void OnStop() => _loggingHost.LogMessage($"[{Name}] OnStop called");

        /// <summary>
        ///     Called when the plugin has been removed by the host application.
        /// </summary>
        public void OnRemove() => _loggingHost.LogMessage($"[{Name}] Onremove called");

        /// <summary>
        ///     Called when the plugin's state has been
        ///     changed by the host application.
        /// </summary>
        /// <param name="state">Plugin's current state</param>
        public void OnStateChanged(PluginState state) =>
            _loggingHost.LogMessage($"[{Name}] Plugin state has changed to '{state}'");

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

        #region ISettingsProperty Event Handlers

        public void OnClick(ISettingsProperty property)
        {
            _loggingHost.LogMessage($"[{Name}] Plugin setting clicked");
        }

        public void OnToggle(ISettingsProperty property)
        {
            _loggingHost.LogMessage($"[{Name}] Plugin setting toggled");
        }

        public void OnSave(ISettingsProperty property)
        {
            _loggingHost.LogMessage($"[{Name}] Plugin setting saved");
        }

        #endregion
    }
}