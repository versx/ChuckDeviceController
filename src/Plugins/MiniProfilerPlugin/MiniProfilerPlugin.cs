namespace ChuckDeviceControllerPluginMvc;

using StackExchange.Profiling;

using ChuckDeviceController.Common;
using ChuckDeviceController.Plugin;

[
    PluginApiKey("CDC-328TVvD7o85TNbNhjLE0JysVMbOxjXKT"),
    StaticFilesLocation(views: StaticFilesLocation.External, webRoot: StaticFilesLocation.External),
]
public class MiniProfilerPlugin : IPlugin
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

    #endregion

    #region Plugin Metadata Properties

    /// <summary>
    /// Gets the name of the plugin to use.
    /// </summary>
    public string Name => "MiniProfilerPlugin";

    /// <summary>
    /// Gets a brief description about the plugin explaining how it
    /// works and what it does.
    /// </summary>
    public string Description => "Demostrates the capabilities of the plugin system using Razor Model-View-Controller (MVC) architecture.";

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
    public MiniProfilerPlugin(ILoggingHost loggingHost)
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
        appBuilder.UseMiniProfiler();
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

        //services.AddMiddlewareAnalysis();

        services.AddMiniProfiler(options =>
        {
            options.RouteBasePath = "/api/profiler";

            //(options.Storage as MemoryCacheStorage).CacheDuration = TimeSpan.FromMinutes(60);

            options.ResultsAuthorize = _ => true;
            options.ResultsListAuthorize = _ => true;

            options.EnableServerTimingHeader = true;

            // (Optional) You can disable "Connection Open()", "Connection Close()" (and async variant) tracking.
            // (defaults to true, and connection opening/closing is tracked)
            options.TrackConnectionOpenClose = true;

            // (Optional) Use something other than the "light" color scheme.
            // (defaults to "light")
            options.ColorScheme = ColorScheme.Auto;

            // (Optional) You can disable MVC filter profiling
            // (defaults to true, and filters are profiled)
            options.EnableMvcFilterProfiling = true;

            // (Optional) You can disable MVC view profiling
            // (defaults to true, and views are profiled)
            options.EnableMvcViewProfiling = true;

            // (Optional) listen to any errors that occur within MiniProfiler itself
            options.OnInternalError = ex => _loggingHost.LogError($"Error: {ex}");

            // (Optional - not recommended) You can enable a heavy debug mode with stacks and tooltips when using memory storage
            // It has a lot of overhead vs. normal profiling and should only be used with that in mind
            // (defaults to false, debug/heavy mode is off)
            options.EnableDebugMode = false;

            options.ShouldProfile = _ => true;

            options.IgnoredPaths.Add("/lib");
            options.IgnoredPaths.Add("/css");
            options.IgnoredPaths.Add("/js");
            options.IgnoredPaths.Add("/swagger");
        });
        //.AddEntityFramework();
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
    public void OnLoad()
    {
        _loggingHost.LogInformation($"{Name} v{Version} by {Author} initialized!");
    }

    /// <summary>
    ///     Called when the plugin has been reloaded by the host application.
    /// </summary>
    public void OnReload()
    {
        _loggingHost.LogInformation($"[{Name}] OnReload called");
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