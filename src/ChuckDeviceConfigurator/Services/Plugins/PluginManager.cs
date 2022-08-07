namespace ChuckDeviceConfigurator.Services.Plugins
{
    using ChuckDeviceConfigurator.Services.Jobs;
    using ChuckDeviceController.Plugins;

    // TODO: Abstract Plugin class inheriting IPlugin for cache plugin related data and to interact with
    // TODO: Add FileSystemWatcher for plugins added manually or changed

    public class PluginManager : IPluginManager
    {
        #region Variables

        private readonly ILogger<IPluginManager> _logger;
        private readonly static Dictionary<string, IPlugin> _plugins = new();

        #endregion

        #region Properties

        public IReadOnlyDictionary<string, IPlugin> Plugins => _plugins;

        public string PluginsFolder { get; set; }

        #endregion

        #region Events

        // TODO: Events for plugins added/removed/stopped/reloaded

        #endregion

        #region Constructors

        public PluginManager(ILogger<IPluginManager> logger)
        {
            _logger = logger;

            PluginsFolder = Strings.PluginsFolder;
        }

        #endregion

        #region Public Methods

        public async Task LoadPluginsAsync(IReadOnlyDictionary<Type, object> sharedHosts)
        {
            if (!Directory.Exists(PluginsFolder))
            {
                throw new DirectoryNotFoundException($"Plugins folder '{PluginsFolder}' does not exist!");
            }

            var pluginFinder = new PluginFinder<IPlugin>(PluginsFolder);
            var pluginFiles = pluginFinder.FindAssemliesWithPlugins();

            foreach (var pluginFile in pluginFiles)
            {
                await LoadPluginAsync(pluginFile, sharedHosts);
            }
        }

        public async Task LoadPluginsAsync(IEnumerable<string> pluginFilePaths, IReadOnlyDictionary<Type, object> sharedHosts)
        {
            foreach (var pluginFile in pluginFilePaths)
            {
                if (!File.Exists(pluginFile))
                {
                    _logger.LogWarning($"Plugin does not exist at '{pluginFile}' unable to load, skipping...");
                    continue;
                }

                await LoadPluginAsync(pluginFile, sharedHosts);
            }
        }


        public async Task StopAsync(string pluginName)
        {
            if (!_plugins.ContainsKey(pluginName))
            {
                Console.WriteLine($"Unable to stop plugin '{pluginName}', no plugin with that name is currently loaded or registered");
                return;
            }

            var plugin = _plugins[pluginName];
            plugin.OnStop();

            await Task.CompletedTask;
        }

        public async Task StopAllAsync()
        {
            foreach (var (_, plugin) in _plugins)
            {
                plugin.OnStop();
            }
            await Task.CompletedTask;
        }

        public async Task ReloadAsync(string pluginName)
        {
            if (!_plugins.ContainsKey(pluginName))
            {
                Console.WriteLine($"Unable to reload plugin '{pluginName}', no plugin with that name is currently loaded or registered");
                return;
            }
            _plugins[pluginName].OnReload();
            await Task.CompletedTask;
        }

        public async Task ReloadAllAsync()
        {
            foreach (var (_, plugin) in _plugins)
            {
                plugin.OnReload();
            }
            await Task.CompletedTask;
        }

        public async Task RemoveAsync(string pluginName)
        {
            if (!_plugins.ContainsKey(pluginName))
            {
                Console.WriteLine($"Unable to remove plugin '{pluginName}', no plugin with that name is currently loaded or registered");
                return;
            }
            _plugins.Remove(pluginName);
            await Task.CompletedTask;
        }

        public async Task RemoveAllAsync()
        {
            foreach (var (pluginName, plugin) in _plugins)
            {
                _plugins.Remove(pluginName);
                plugin.OnRemove();
            }
            await Task.CompletedTask;
        }

        #endregion

        #region Private Methods

        private async Task LoadPluginAsync(string pluginFilePath, IReadOnlyDictionary<Type, object> sharedHosts)
        {
            /*
            var sharedHosts = new Dictionary<Type, object>
            {
                { typeof(IJobControllerServiceHost), _jobControllerService },
                { typeof(ILoggingHost), _loggingHost },
                { typeof(IUiHost), _uiHost },
                { typeof(IDatabaseHost), _databaseHost },
            };
            */
            var loader = new PluginLoader<IPlugin>(pluginFilePath, sharedHosts);
            var loadedPlugins = loader.LoadedPlugins;
            if (!loadedPlugins.Any())
            {
                _logger.LogWarning($"No plugins loaded from plugin file '{pluginFilePath}'");
                return;
            }

            foreach (var plugin in loadedPlugins)
            {
                RegisterPlugin(plugin);

                plugin.OnLoad();
            }

            await Task.CompletedTask;
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

        // TODO: UnregisterPlugin when removed

        #endregion
    }
}