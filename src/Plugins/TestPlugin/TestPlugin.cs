namespace TestPlugin
{
    using ChuckDeviceController.Common.Data;
    using ChuckDeviceController.Plugins;

    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.DependencyInjection;

    //http://127.0.0.1:8881/plugin/v1
    //http://127.0.0.1:8881/Test

    /// <summary>
    /// Example plugin demonstrating the capabilities of
    /// the plugin system and how it works.
    /// </summary>
    public class TestPlugin : IPlugin
    {
        #region Plugin Host Variables

        // Plugin host variables are interface contracts that the plugin
        // to interact with services the host application is running.
        private readonly ILoggingHost _loggingHost;
        private readonly IJobControllerServiceHost _jobControllerHost;

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
        public TestPlugin(ILoggingHost loggingHost, IJobControllerServiceHost jobControllerHost)
        {
            _loggingHost = loggingHost;
            _jobControllerHost = jobControllerHost;

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
        public void Configure(IApplicationBuilder appBuilder)
        {
            _loggingHost.LogMessage($"Configure called");

            // We can configure routing here or using Mvc Controller classes
            appBuilder.Map("/plugin/v1", app =>
            {
                app.Run(async (httpContext) =>
                {
                    Console.WriteLine($"Plugin route called");
                    await httpContext.Response.WriteAsync($"Hello from plugin {Name}");
                });
            });
        }

        /// <summary>
        /// Register services into the IServiceCollection to use with Dependency Injection.
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
            // TODO: Add/register TestInstanceController
            _jobControllerHost.RegisterJobControllerTypeAsync(InstanceType)
        }

        /// <summary>
        /// Called when the plugin has been reloaded by the host application.
        /// </summary>
        public void OnReload()
        {
            _loggingHost.LogMessage($"OnReload called from plugin");
        }

        /// <summary>
        /// Called when the plugin has been stopped by the host application.
        /// </summary>
        public void OnStop()
        {
            _loggingHost.LogMessage($"OnStop called from plugin");
        }

        /// <summary>
        /// Called when the plugin has been removed by the host application.
        /// </summary>
        public void OnRemove()
        {
            _loggingHost.LogMessage($"OnRemove called from plugin");
        }

        #endregion
    }

    public class TestPluginService : IPluginService
    {
        public string Test => $"Testing";
    }
}