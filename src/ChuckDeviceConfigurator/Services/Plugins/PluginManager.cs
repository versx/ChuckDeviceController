namespace ChuckDeviceConfigurator.Services.Plugins
{
    using ChuckDeviceController.Common.Data;
    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Data.Factories;
    using ChuckDeviceController.Plugins;

    // TODO: Add FileSystemWatcher for plugins added manually or changed

    public class PluginManager : IPluginManager // TODO: <TPlugin>
    {
        #region Variables

        private static readonly ILogger<IPluginManager> _logger =
            new Logger<IPluginManager>(LoggerFactory.Create(x => x.AddConsole()));
        private static IPluginManager _instance;
        private static readonly Dictionary<string, IPluginHost> _plugins = new();

        #endregion

        #region Properties

        public static IPluginManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new PluginManager(new PluginManagerOptions
                    {
                        RootPluginDirectory = Strings.PluginsFolder,
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
                    RootPluginDirectory = Strings.PluginsFolder,
                    SharedServiceHosts = sharedServiceHosts,
                    Configuration = configuration,
                });
            }
            return _instance;
        }

        public IReadOnlyDictionary<string, IPluginHost> Plugins => _plugins;

        public IPluginManagerOptions Options { get; }

        public IPluginHost this[string key] => _plugins.ContainsKey(key)
            ? _plugins[key]
            : null;

        #endregion

        #region Constructors

        public PluginManager(IPluginManagerOptions options)
        {
            Options = options;
        }

        #endregion

        #region Public Methods

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

            // Cache plugin host state in database
            var state = await CachePluginHostStateAsync(pluginHost);
            if (state.State != PluginState.Running)
            {
                _logger.LogWarning($"Plugin '{state.Name}' state was previously set to '{state.State}'.");
            }

            _plugins.Add(plugin.Name, pluginHost);
            _logger.LogInformation($"Plugin '{plugin.Name}' v{plugin.Version} by {plugin.Author} initialized and registered to plugin manager cache.");
        }

        public async Task StopAsync(string pluginName)
        {
            if (!_plugins.ContainsKey(pluginName))
            {
                _logger.LogWarning($"Unable to stop plugin '{pluginName}', no plugin with that name is currently loaded or registered");
                return;
            }

            var pluginHost = _plugins[pluginName];
            pluginHost.Plugin.OnStop();
            pluginHost.SetState(PluginState.Stopped);

            await SaveStateAsync(pluginName, PluginState.Removed);

            _logger.LogInformation($"[{pluginName}] Plugin has been stopped");
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
            pluginHost.Plugin.OnReload();
            pluginHost.SetState(PluginState.Running);

            await SaveStateAsync(pluginName, PluginState.Running);

            _logger.LogInformation($"[{pluginName}] Plugin has been reloaded");
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
            pluginHost.Unload();
            pluginHost.Plugin.OnRemove();
            pluginHost.SetState(PluginState.Removed);
            _plugins.Remove(pluginName);

            await SaveStateAsync(pluginName, PluginState.Removed);

            _logger.LogInformation($"[{pluginName}] Plugin has been removed");
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

            // Save plugin state to database
            await SaveStateAsync(pluginName, state);

            if (state == PluginState.Running &&
                previousState != PluginState.Running &&
                state != previousState)
            {
                // Call 'OnLoad' event in case plugin was disabled upon startup,
                // which none of the UI elements were registered. (if it has any)
                pluginHost.Plugin.OnLoad();
            }
        }

        #endregion

        #region Private Methods

        private static async Task<Plugin> CachePluginHostStateAsync(PluginHost pluginHost)//, PluginState state = PluginState.Unset)
        {
            // TODO: Get IConfiguration
            var connectionString = Instance.Options.Configuration.GetConnectionString("DefaultConnection");
            using (var context = DbContextFactory.CreateControllerContext(connectionString))
            {
                // Get cached plugin state from database
                var dbPlugin = await context.Plugins.FindAsync(pluginHost.Plugin.Name);
                if (dbPlugin != null)
                {
                    // Plugin host is cached in database, set previous plugin state,
                    // otherwise set state from param
                    //var isStateSet = state != dbPlugin.State && state != PluginState.Unset;
                    //pluginHost.SetState(isStateSet ? state : dbPlugin.State);
                    pluginHost.SetState(dbPlugin.State);
                    return dbPlugin;
                }
                else
                {
                    // Plugin host is not cached in database. Set current state to plugin
                    // host and add insert into database
                    pluginHost.SetState(PluginState.Running);
                    dbPlugin = new Plugin
                    {
                        Name = pluginHost.Plugin.Name,
                        State = pluginHost.State,
                    };
                }

                // Save plugin host to database
                if (context.Plugins.Any(x => x.Name == pluginHost.Plugin.Name))
                {
                    context.Plugins.Update(dbPlugin);
                }
                else
                {
                    await context.Plugins.AddAsync(dbPlugin);
                }
                await context.SaveChangesAsync();

                return dbPlugin;
            }
        }

        private async Task SaveStateAsync(string pluginName, PluginState state)
        {
            var connectionString = Options.Configuration.GetConnectionString("DefaultConnection");
            using (var context = DbContextFactory.CreateControllerContext(connectionString))
            {
                // Get cached plugin state from database
                var plugin = await context.Plugins.FindAsync(pluginName);
                if (plugin == null)
                {
                    _logger.LogError($"Failed to find plugin with name '{pluginName}' in database");
                    return;
                }

                plugin.State = state;

                // Save plugin host to database
                if (context.Plugins.Any(x => x.Name == pluginName))
                {
                    context.Plugins.Update(plugin);
                }
                else
                {
                    await context.Plugins.AddAsync(plugin);
                }
                await context.SaveChangesAsync();
            }
        }

        #endregion
    }
}