﻿namespace ChuckDeviceConfigurator.Services.Plugins
{
    using ChuckDeviceController.Common.Data;
    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Plugins;

    // TODO: Keep track of plugin states in database or local file
    // TODO: Add FileSystemWatcher for plugins added manually or changed

    public class PluginManager : IPluginManager
    {
        #region Variables

        private readonly ILogger<IPluginManager> _logger;
        private readonly static Dictionary<string, IPluginHost> _plugins = new();
        private readonly ControllerContext _context;

        #endregion

        #region Properties

        public IReadOnlyDictionary<string, IPluginHost> Plugins => _plugins;

        public string PluginsFolder { get; set; }

        #endregion

        #region Events

        // TODO: Events for plugins added/removed/stopped/reloaded

        #endregion

        #region Constructors

        public PluginManager(
            ILogger<IPluginManager> logger,
            ControllerContext context)
        {
            _logger = logger;
            _context = context;

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
                _logger.LogWarning($"Unable to stop plugin '{pluginName}', no plugin with that name is currently loaded or registered");
                return;
            }

            var pluginHost = _plugins[pluginName];
            pluginHost.Plugin.OnStop();
            pluginHost.Plugin.OnStateChanged(PluginState.Stopped);

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
            pluginHost.Plugin.OnReload();
            pluginHost.Plugin.OnStateChanged(PluginState.Running);

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
            pluginHost.Plugin.OnRemove();
            pluginHost.Plugin.OnStateChanged(PluginState.Removed);
            _plugins.Remove(pluginName);

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

        #endregion

        #region Private Methods

        private async Task LoadPluginAsync(string pluginFilePath, IReadOnlyDictionary<Type, object> sharedHosts)
        {
            var loader = new PluginLoader<IPlugin>(pluginFilePath, sharedHosts);
            var loadedPlugins = loader.LoadedPlugins;
            if (!loadedPlugins.Any())
            {
                _logger.LogWarning($"No plugins loaded from plugin file '{pluginFilePath}'");
                return;
            }

            // Loop all valid plugins found within assembly
            foreach (var plugin in loadedPlugins)
            {
                await RegisterPluginAsync(plugin);

                // TODO: Add requested plugin permissions to cache and show list in dashboard
                // to accept plugin permissions request or just allow it regardless? or add
                // config option to set which permissions plugins are allowed? idk

                if (plugin.State == PluginState.Running)
                {
                    // Only call plugin 'OnLoad' event if the plugin in the enabled/running state.
                    plugin.Plugin.OnLoad();
                }
            }
        }

        private async Task RegisterPluginAsync(IPluginHost pluginHost)
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

        private async Task<Plugin> CachePluginHostStateAsync(IPluginHost pluginHost)
        {
            // Get cached plugin state from database
            var dbPlugin = await _context.Plugins.FindAsync(pluginHost.Plugin.Name);
            if (dbPlugin != null)
            {
                // Plugin host is cached in database, set previous plugin state
                pluginHost.SetState(dbPlugin.State);
                return null;
            }

            // Plugin host is not cached in database. Set current state to plugin
            // host and add insert into database
            pluginHost.SetState(PluginState.Running);
            dbPlugin = new Plugin
            {
                Name = pluginHost.Plugin.Name,
                State = pluginHost.State,
            };

            // Save plugin host to database
            await _context.Plugins.AddAsync(dbPlugin);
            await _context.SaveChangesAsync();

            return dbPlugin;
        }

        #endregion
    }
}