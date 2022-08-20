namespace ChuckDeviceController.PluginManager
{
    using Microsoft.AspNetCore.Builder;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;

    using ChuckDeviceController.Common.Data;

    // TODO: Add FileSystemWatcher for plugins added manually or changed

    public class PluginManager : IPluginManager
    {
        public const string DefaultPluginsFolder = "./bin/debug/plugins/";

        #region Variables

        private static readonly ILogger<IPluginManager> _logger =
            new Logger<IPluginManager>(LoggerFactory.Create(x => x.AddConsole()));
        private static IPluginManager? _instance;
        private static readonly Dictionary<string, IPluginHost> _plugins = new();

        #endregion

        #region Singleton

        public static IPluginManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new PluginManager(new PluginManagerOptions
                    {
                        RootPluginDirectory = DefaultPluginsFolder,
                    });
                }
                return _instance;
            }
        }

        //public static IPluginManager InstanceWithOptions(IPluginManagerOptions options, IConfiguration? configuration = null, IServiceCollection? sharedServiceHosts = null)
        public static IPluginManager InstanceWithOptions(IPluginManagerOptions options, IConfiguration? configuration = null, IReadOnlyDictionary<Type, object>? sharedServiceHosts = null)
        {
            if (_instance == null)
            {
                _instance = new PluginManager(options ?? new PluginManagerOptions
                {
                    RootPluginDirectory = DefaultPluginsFolder,
                    SharedServiceHosts = sharedServiceHosts,
                    Configuration = configuration,
                });
            }
            return _instance;
        }

        #endregion

        #region Properties

        public IReadOnlyDictionary<string, IPluginHost> Plugins => _plugins;

        public IPluginManagerOptions Options { get; }

        public IPluginHost? this[string key] => _plugins?.ContainsKey(key) ?? false
            ? _plugins[key]
            : null;

        #endregion

        #region Events

        public event EventHandler<PluginHostAddedEventArgs>? PluginHostAdded;
        private void OnPluginHostAdded(IPluginHost pluginHost)
        {
            PluginHostAdded?.Invoke(this, new PluginHostAddedEventArgs(pluginHost));
        }

        public event EventHandler<PluginHostRemovedEventArgs>? PluginHostRemoved;
        private void OnPluginHostRemoved(IPluginHost pluginHost)
        {
            PluginHostRemoved?.Invoke(this, new PluginHostRemovedEventArgs(pluginHost));
        }

        public event EventHandler<PluginHostStateChangedEventArgs>? PluginHostStateChanged;
        private void OnPluginHostStateChanged(IPluginHost pluginHost, PluginState previousState)
        {
            PluginHostStateChanged?.Invoke(this, new PluginHostStateChangedEventArgs(pluginHost, previousState));
        }

        #endregion

        #region Constructors

        public PluginManager(IPluginManagerOptions options)
        {
            Options = options;
        }

        #endregion

        #region Public Methods

        public void Configure(WebApplication app)
        {
            try
            {
                foreach (var (_, plugin) in Plugins)
                {
                    // Call 'Configure(IApplicationBuilder)' event handler for each plugin
                    plugin.Plugin.Configure(app);

                    // TODO: Call Plugin.OnLoad() here instead of from ServiceCollectionExtensions
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while calling the 'Configure(IApplicationBuilder)' method in plugins.");
            }
        }

        public async Task RegisterPluginAsync(PluginHost pluginHost)
        {
            var plugin = pluginHost.Plugin;
            if (_plugins.ContainsKey(plugin.Name))
            {
                // Check if version is higher than current, if so replace existing
                var oldVersion = _plugins[pluginHost.Plugin.Name].Plugin.Version;
                var newVersion = plugin.Version;
                if (oldVersion > newVersion)
                {
                    // Existing is newer
                    _logger.LogWarning($"Existing plugin version with name '{plugin.Name}' is newer than incoming plugin version, skipping registration...");
                    return;
                }
                else if (oldVersion < newVersion)
                {
                    // Incoming is newer
                    _logger.LogWarning($"Existing plugin version with name '{plugin.Name}' is newer than incoming plugin version, removing old version and adding new version...");

                    // Remove existing plugin so we can add newer version of plugin
                    await RemoveAsync(plugin.Name);
                }
                else
                {
                    // Plugin versions are the same
                    _logger.LogWarning($"Existing plugin version with name '{plugin.Name}' is the same version as incoming plugin version, skipping registration...");
                    return;
                }
            }

            _plugins.Add(plugin.Name, pluginHost);
            _logger.LogInformation($"Plugin '{plugin.Name}' v{plugin.Version} by {plugin.Author} initialized and registered to plugin manager cache.");

            OnPluginHostAdded(pluginHost);
        }

        public async Task StopAsync(string pluginName)
        {
            if (!_plugins.ContainsKey(pluginName))
            {
                _logger.LogWarning($"Unable to stop plugin '{pluginName}', no plugin with that name is currently loaded or registered");
                return;
            }

            var pluginHost = _plugins[pluginName];
            var previousState = pluginHost.State;
            pluginHost.Plugin.OnStop();
            pluginHost.SetState(PluginState.Stopped);

            OnPluginHostStateChanged(pluginHost, previousState);
            //await SaveStateAsync(pluginName, PluginState.Removed);

            _logger.LogInformation($"[{pluginName}] Plugin has been stopped");
            await Task.CompletedTask;
        }

        public async Task StopAllAsync()
        {
            foreach (var (pluginName, _) in _plugins)
            {
                await StopAsync(pluginName);
            }
        }

        public async Task ReloadAsync(string pluginName)
        {
            if (!_plugins.ContainsKey(pluginName))
            {
                _logger.LogWarning($"Unable to reload plugin '{pluginName}', no plugin with that name is currently loaded or registered");
                return;
            }

            // TODO: Reload - Remove and add plugin

            var pluginHost = _plugins[pluginName];
            var previousState = pluginHost.State;
            pluginHost.Plugin.OnReload();
            pluginHost.SetState(PluginState.Running);

            OnPluginHostStateChanged(pluginHost, previousState);
            //await SaveStateAsync(pluginName, PluginState.Running);

            _logger.LogInformation($"[{pluginName}] Plugin has been reloaded");
            await Task.CompletedTask;
        }

        public async Task ReloadAllAsync()
        {
            foreach (var (pluginName, _) in _plugins)
            {
                await ReloadAsync(pluginName);
            }
        }

        public async Task RemoveAsync(string pluginName)
        {
            if (!_plugins.ContainsKey(pluginName))
            {
                _logger.LogWarning($"Unable to remove plugin '{pluginName}', no plugin with that name is currently loaded or registered");
                return;
            }

            var pluginHost = _plugins[pluginName];
            var previousState = pluginHost.State;
            pluginHost.Unload();
            pluginHost.Plugin.OnRemove();
            pluginHost.SetState(PluginState.Removed);
            _plugins.Remove(pluginName);

            OnPluginHostStateChanged(pluginHost, previousState);
            OnPluginHostRemoved(pluginHost);
            //await SaveStateAsync(pluginName, PluginState.Removed);

            _logger.LogInformation($"[{pluginName}] Plugin has been removed");
            await Task.CompletedTask;
        }

        public async Task RemoveAllAsync()
        {
            foreach (var (pluginName, _) in _plugins)
            {
                await RemoveAsync(pluginName);
            }
        }

        public async Task SetStateAsync(string pluginName, PluginState state)
        {
            if (!_plugins.ContainsKey(pluginName))
            {
                _logger.LogError($"Failed to set plugin state, plugin with name '{pluginName}' does not exist in cache");
                return;
            }

            // Set state in plugin cache
            var pluginHost = _plugins[pluginName];
            var previousState = pluginHost.State;
            pluginHost.SetState(state);

            // Inform host plugin state has changed and to update database cache
            OnPluginHostStateChanged(pluginHost, previousState);

            if (state == PluginState.Running &&
                previousState != PluginState.Running &&
                state != previousState)
            {
                // Call 'OnLoad' event in case plugin was disabled upon startup,
                // which none of the UI elements were registered. (if it has any)
                pluginHost.Plugin.OnLoad();
            }

            await Task.CompletedTask;
        }

        public IEnumerable<string> GetPluginFolderNames()
        {
            var pluginFolderNames = Plugins.Values
                                           .Select(plugin => Path.GetDirectoryName(plugin.PluginFinderResult.FullAssemblyPath))
                                           .Select(plugin => Path.GetFileName(plugin));
            return pluginFolderNames;
        }

        #endregion
    }
}