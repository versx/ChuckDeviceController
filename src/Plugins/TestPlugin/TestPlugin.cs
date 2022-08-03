namespace TestPlugin
{
    using System.Threading.Tasks;

    using ChuckDeviceController.Plugins;

    public class TestPlugin : IPlugin, IAppEvents
    {
        #region Metadata Properties

        public string Name => "TestPlugin";

        public string Description => "Demostrates capabilities of plugin system";

        public string Author => "versx";

        public string Version => "1.0.0.0";

        #endregion

        private readonly IAppHost _appHost;
        private readonly ILoggingHost _loggingHost;

        public TestPlugin(IAppHost appHost, ILoggingHost loggingHost)
        {
            _appHost = appHost;
            _loggingHost = loggingHost;

            _appHost.Restart();
        }

        public async Task InitializeAsync()
        {
            _loggingHost.LogMessage($"{Name} v{Version} by {Author} initialized!");
            await Task.CompletedTask;
        }

        public Task OnInitializedAsync()
        {
            _loggingHost.LogMessage($"OnInitializedAsync called from plugin");
            return Task.CompletedTask;
        }

        public Task OnStopAsync()
        {
            _loggingHost.LogMessage($"OnStopAsync called from plugin");
            return Task.CompletedTask;
        }
    }

    /*
    public class MyPluginController : Controller
    {
        public IActionResult Index() => View();
    }
    */
}