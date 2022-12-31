namespace HealthChecksPlugin;

using System.Diagnostics;

using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

using ChuckDeviceController.Data.Common;
using ChuckDeviceController.Plugin;

// Reference: https://github.com/Xabaril/AspNetCore.Diagnostics.HealthChecks/blob/master/samples/HealthChecks.UIAndApi/Startup.cs

[
    PluginApiKey("CDC-328TVvD7o85TNbNhjLE0JysVMbOxjXKT"),
    StaticFilesLocation(views: StaticFilesLocation.External, webRoot: StaticFilesLocation.External),
]
public class HealthChecksPlugin : IPlugin
{
    #region Plugin Host Variables

    private readonly ILoggingHost _loggingHost;

    private readonly IUiHost _uiHost;

    #endregion

    #region Plugin Metadata Properties

    /// <summary>
    /// Gets the name of the plugin to use.
    /// </summary>
    public string Name => "HealthChecksPlugin";

    /// <summary>
    /// Gets a brief description about the plugin explaining how it
    /// works and what it does.
    /// </summary>
    public string Description => "";

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

    public HealthChecksPlugin(ILoggingHost loggingHost, IUiHost uiHost)
    {
        _loggingHost = loggingHost;
        _uiHost = uiHost;
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
        try
        {
            //appBuilder.UseHealthChecks("/api/health");
            appBuilder
                .UseRouting()
                .UseEndpoints(config =>
                {
                    config.MapControllers();
                    config.MapHealthChecks("/health"); // api/health
                    //config.MapHealthChecksUI();
                    //config.MapHealthChecks("/api/health");
                    config.MapHealthChecksUI(options =>
                    {
                        options.UIPath = "/health-ui";
                        //options.ApiPath = "/api/health";
                        options.ResourcesPath = "/health";
                    });
                });

            //appBuilder.UseHealthChecks("/health", new HealthCheckOptions
            //{
            //    //Predicate = p => true,
            //    //ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
            //});
            //appBuilder.UseHealthChecksUI();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex}");
        }
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
        try
        {
            services.AddHealthChecks();
            //services.AddControllers();
            services
                .AddHealthChecksUI(options =>
                {
                    // Sets the Health Check endpoint
                    options.AddHealthCheckEndpoint("Health Checks API", "/health");
                    // Sets the time interval in which HealthCheck will be triggered
                    //options.SetEvaluationTimeInSeconds(5);
                    // Sets the maximum number of records displayed in history
                    options.MaximumHistoryEntriesPerEndpoint(10);
                })
                .AddInMemoryStorage();
            //.AddWorkingSetHealthCheck(1024 * 1024 * 1024, "Process Working Set")
            //.AddProcessHealthCheck(Process.GetCurrentProcess().ProcessName, p => p.Length >= 1, "Application Process")
            //.AddProcessAllocatedMemoryHealthCheck((int)Environment.WorkingSet)
            //.AddDiskStorageHealthCheck(options =>
            //{
            //    options.CheckAllDrives = true;
            //    foreach (var drive in DriveInfo.GetDrives())
            //    {
            //        if (drive.IsReady && drive.TotalSize > 0 && drive.DriveType == DriveType.Fixed)
            //        {
            //            options.AddDrive(drive.RootDirectory.Name, 1024);
            //        }
            //    }
            //});

            //services
            //     .AddHealthChecksUI()
            //     .AddInMemoryStorage();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex}");
        }
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