namespace ChuckDeviceConfigurator.Services.Plugins
{
    using System;

    using ChuckDeviceController.Plugins;

    // TODO: Abstract Plugin class inheriting IPlugin for cache plugin related data and to interact with

    public class PluginManager : IPluginManager
    {
        #region Variables

        private readonly ILogger<IPluginManager> _logger;
        private readonly Dictionary<string, IPlugin> _plugins = new();

        #endregion

        #region Singleton

        private static IPluginManager? _instance;
        public static IPluginManager Instance => _instance ??= new PluginManager();

        #endregion

        #region Properties

        public IReadOnlyDictionary<string, IPlugin> Plugins => _plugins;

        public string PluginsFolder { get; set; }

        #endregion

        #region Events

        // TODO: Events for plugins added/removed/stopped/reloaded

        #endregion

        #region Constructors

        public PluginManager()
            : this(new Logger<IPluginManager>(LoggerFactory.Create(x => x.AddConsole())))
        {
        }

        public PluginManager(ILogger<IPluginManager> logger)
        {
            _logger = logger;

            PluginsFolder = Strings.PluginsFolder;
        }

        #endregion

        #region Public Methods

        public async Task LoadPluginsAsync()
        {
            if (!Directory.Exists(PluginsFolder))
            {
                throw new DirectoryNotFoundException($"Plugins folder '{PluginsFolder}' does not exist!");
            }

            var pluginFinder = new PluginFinder<IPlugin>(PluginsFolder);
            var pluginFiles = pluginFinder.FindAssemliesWithPlugins();

            foreach (var pluginFile in pluginFiles)
            {
                await LoadPluginAsync(pluginFile);
            }
        }

        public async Task LoadPluginsAsync(IEnumerable<string> pluginFilePaths)
        {
            foreach (var pluginFile in pluginFilePaths)
            {
                if (!File.Exists(pluginFile))
                {
                    _logger.LogWarning($"Plugin does not exist at '{pluginFile}' unable to load, skipping...");
                    continue;
                }

                await LoadPluginAsync(pluginFile);
            }
        }


        public async Task StopAsync(string pluginName)
        {
            if (!_plugins.ContainsKey(pluginName))
            {
                Console.WriteLine($"Unable to stop plugin '{pluginName}', no plugin with that name is currently loaded or registered");
                return;
            }

            // TODO: Call IAppEvents.OnStopped

            await Task.CompletedTask;
        }

        public async Task StopAllAsync()
        {
            foreach (var plugin in _plugins)
            {
                // TODO: Call IAppEvents.OnStopped
            }

            await Task.CompletedTask;
        }

        public async Task UnloadAsync(string pluginName)
        {
            if (!_plugins.ContainsKey(pluginName))
            {
                Console.WriteLine($"Unable to unload plugin '', no plugin with that name is currently loaded or registered");
                return;
            }

            // TODO: Call IAppEvents.OnUnloaded
            _plugins.Remove(pluginName);
            await Task.CompletedTask;
        }

        public async Task UnloadAllAsync()
        {
            foreach (var plugin in _plugins)
            {
                // TODO: Call IAppEvents.OnStopped
            }
            _plugins.Clear();
            await Task.CompletedTask;
        }

        #endregion

        #region Private Methods

        private async Task LoadPluginAsync(string pluginFilePath)
        {
            var loader = new PluginLoader<IPlugin>(pluginFilePath);
            var loadedPlugins = loader.LoadedPlugins;
            if (!loadedPlugins.Any())
            {
                _logger.LogWarning($"No plugins loaded from plugin file '{pluginFilePath}'");
                return;
            }

            foreach (var plugin in loadedPlugins)
            {
                await plugin.InitializeAsync();
                RegisterPlugin(plugin);
            }
        }

        private void RegisterPlugin(IPlugin plugin)
        {
            if (_plugins.ContainsKey(plugin.Name))
            {
                // TODO: Check if version is higher than current, if so replace existing instead of skipping
                _logger.LogWarning($"Plugin with name '{plugin.Name}' already loaded and registered, skipping...");
                return;
            }

            _plugins.Add(plugin.Name, plugin);
            _logger.LogInformation($"Plugin '{plugin.Name}' v{plugin.Version} by {plugin.Author} initialized and registered to plugin manager cache.");
        }

        #endregion
    }

    #region Mock Host Classes (for now)

    public class AppHost : IAppHost
    {
        public void Restart()
        {
            Console.WriteLine($"Restart");
        }

        public void Shutdown()
        {
            Console.WriteLine($"Shutdown");
        }

        public void Uninstall()
        {
            Console.WriteLine($"Uninstall");
        }
    }

    public class LoggingHost : ILoggingHost
    {
        public void LogException(Exception ex)
        {
            Console.WriteLine($"Exception occurred from plugin: {ex}");
        }

        public void LogMessage(string text, params object[] args)
        {
            Console.WriteLine($"Message from plugin: {text}", args);
        }
    }

    public class UiHost : IUiHost
    {
        public Task AddNavbarHeaderAsync(NavbarHeaderOptions options)
        {
            return Task.CompletedTask;
        }

        public Task AddPathAsync()
        {
            return Task.CompletedTask;
        }
    }

    public class DatabaseHost : IDatabaseHost
    {
    }

    #endregion
}