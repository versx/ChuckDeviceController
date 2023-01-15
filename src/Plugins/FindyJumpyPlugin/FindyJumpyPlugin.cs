///
/// Credits:
/// - https://github.com/RealDeviceMap/RealDeviceMap/pull/437
/// - https://github.com/madBeavis
///
namespace FindyJumpyPlugin;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using ChuckDeviceController.Common;
using ChuckDeviceController.Plugin;

using JobControllers;

/// <summary>
///     Example plugin demonstrating the capabilities
///     of the plugin system and how it works.
/// </summary>
[
    // Specifies where the 'wwwroot' folder will be if any are used or needed.
    // Possible options: embedded resources, local/external, or none.
    StaticFilesLocation(
        views: StaticFilesLocation.None,
        webRoot: StaticFilesLocation.None,
        pages: StaticFilesLocation.None
    ),
    // Specify the plugin API key to authorize with the host application.
    PluginApiKey("CDC-328TVvD7o85TNbNhjLE0JysVMbOxjXKT"),
]
public class FindyJumpyPlugin : IPlugin, IJobControllerServiceEvents
{
    private const string FindyName = "Findy";
    private const string JumpyName = "Jumpy";

    #region Variables

    private readonly ILogger<FindyJumpyPlugin> _logger;
    private readonly IJobControllerServiceHost _jobControllerHost;

    #endregion

    #region Plugin Metadata Properties

    /// <summary>
    /// Gets the name of the plugin to use.
    /// </summary>
    public string Name => "FindyJumpyPlugin";

    /// <summary>
    /// Gets a brief description about the plugin explaining how it
    /// works and what it does.
    /// </summary>
    public string Description => "Adds new spawnpoint based job controller instances.";

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
    /// <param name="logger">Logger instance.</param>
    /// <param name="jobControllerHost">Job controller service host handler.</param>
    public FindyJumpyPlugin(
        ILogger<FindyJumpyPlugin> logger,
        IJobControllerServiceHost jobControllerHost)
    {
        _logger = logger;
        _jobControllerHost = jobControllerHost;
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
    }

    /// <summary>
    ///     Provides an opportunity for plugins to configure Mvc Builder.
    /// </summary>
    /// <param name="mvcBuilder">
    ///     IMvcBuilder instance that can be configured.
    /// </param>
    public void ConfigureMvcBuilder(IMvcBuilder mvcBuilder)
    {
    }

    #endregion

    #region Plugin Event Handlers

    /// <summary>
    ///     Called when the plugin is loaded and registered with the host application.
    ///     Loading UI elements here is the preferred location.
    /// </summary>
    public async void OnLoad()
    {
        _logger.LogInformation("{Name} v{Version} by {Author} initialized!", Name, Version, Author);

        // Register custom job controller type FindyJobController
        await _jobControllerHost.RegisterJobControllerAsync<FindyJobController>(FindyName);

        // Register custom job controller type JumpyJobController
        await _jobControllerHost.RegisterJobControllerAsync<JumpyJobController>(JumpyName);
    }

    /// <summary>
    ///     Called when the plugin has been reloaded by the host application.
    /// </summary>
    public void OnReload() => _logger.LogInformation("[{Name}] OnReload called", Name);

    /// <summary>
    ///     Called when the plugin has been stopped by the host application.
    /// </summary>
    public void OnStop() => _logger.LogInformation("[{Name}] OnStop called", Name);

    /// <summary>
    ///     Called when the plugin has been removed by the host application.
    /// </summary>
    public void OnRemove() => _logger.LogInformation("[{Name}] Onremove called", Name);

    /// <summary>
    ///     Called when the plugin's state has been
    ///     changed by the host application.
    /// </summary>
    /// <param name="state">Plugin's current state</param>
    public void OnStateChanged(PluginState state) =>
        _logger.LogInformation("[{Name}] Plugin state has changed to '{state}'", Name, state);

    #endregion
}