namespace TestPlugin
{
    using System.Threading.Tasks;

    using ChuckDeviceController.Plugins;

    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.DependencyInjection;

    //http://127.0.0.1:8881/plugin/v1
    //http://127.0.0.1:8881/Test

    public class TestPlugin : IPlugin
    {
        #region Metadata Properties

        public string Name => "TestPlugin";

        public string Description => "Demostrates the capabilities of the plugin system.";

        public string Author => "versx";

        public Version Version => new Version("1.0.0.0");

        #endregion

        private readonly ILoggingHost _loggingHost;
        private readonly IJobControllerServiceHost _jobControllerHost;

        public TestPlugin(ILoggingHost loggingHost, IJobControllerServiceHost jobControllerHost)
        {
            _loggingHost = loggingHost;
            _jobControllerHost = jobControllerHost;

            //_appHost.Restart();
        }

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

        public void ConfigureServices(IServiceCollection services)
        {
            _loggingHost.LogMessage($"ConfigureServices called");
            services.AddSingleton<IPluginService, TestPluginService>();
            //services.AddMvc();
            //services.AddControllersWithViews();
        }

        public void OnLoad()
        {
            _loggingHost.LogMessage($"{Name} v{Version} by {Author} initialized!");
        }

        public void OnReload()
        {
            _loggingHost.LogMessage($"OnReload called from plugin");
        }

        public void OnStop()
        {
            _loggingHost.LogMessage($"OnStop called from plugin");
        }

        public void OnRemove()
        {
            _loggingHost.LogMessage($"OnRemove called from plugin");
        }
    }

    public class TestPluginService : IPluginService
    {
        public string Test => $"Testing";
    }
}