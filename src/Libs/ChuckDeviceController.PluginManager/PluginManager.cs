namespace ChuckDeviceController.PluginManager
{
    using System.Reflection;

    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Mvc.Razor.Compilation;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;

    using ChuckDeviceController.Common.Data;
    using ChuckDeviceController.Plugin;
    using ChuckDeviceController.PluginManager.Mvc.Extensions;
    using ChuckDeviceController.PluginManager.Mvc.Razor;
    using ChuckDeviceController.PluginManager.Services.Finder;
    using ChuckDeviceController.PluginManager.Services.Loader;

    // TODO: Add FileSystemWatcher for plugins added manually or changed

    public class PluginManager : IPluginManager
    {
        public const string DefaultPluginsFolder = "./bin/debug/plugins/";

        #region Variables

        private static readonly ILogger<IPluginManager> _logger =
            new Logger<IPluginManager>(LoggerFactory.Create(x => x.AddConsole()));
        private static IPluginManager? _instance;
        private static readonly Dictionary<string, IPluginHost> _plugins = new();
        private IServiceCollection _services;
        private IWebHostEnvironment _webHostEnv;

        #endregion

        #region Singleton

        public static IPluginManager Instance => _instance ??= new PluginManager
        (
            new PluginManagerOptions
            {
                RootPluginsDirectory = DefaultPluginsFolder,
            }
        );

        public static IPluginManager InstanceWithOptions(
            IPluginManagerOptions options,
            IConfiguration? configuration = null,
            IServiceCollection? services = null,
            IReadOnlyDictionary<Type, object>? sharedServiceHosts = null)
        {
            if (_instance == null)
            {
                _instance = new PluginManager(options ?? new PluginManagerOptions
                {
                    RootPluginsDirectory = DefaultPluginsFolder,
                    Configuration = configuration,
                    Services = services,
                    SharedServiceHosts = sharedServiceHosts,
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

        public IServiceCollection Services => _services;

        public IWebHostEnvironment WebHostEnv => _webHostEnv;

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
            foreach (var (pluginName, plugin) in Plugins)
            {
                try
                {
                    // Call 'Configure(IApplicationBuilder)' event handler for each plugin
                    plugin.Plugin.Configure(app);

                    // TODO: Call Plugin.OnLoad() here instead of from ServiceCollectionExtensions
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"An error occurred while calling the 'Configure(IApplicationBuilder)' method for plugin '{pluginName}'.");
                }
            }
        }

        public async Task<IServiceCollection> LoadPluginsAsync(IServiceCollection services, IWebHostEnvironment env)
        {
            _services = services;
            _webHostEnv = env;

            var rootPluginsDirectory = Options.RootPluginsDirectory;
            var finderOptions = new PluginFinderOptions
            {
                PluginType = typeof(IPlugin),
                RootPluginsDirectory = rootPluginsDirectory,
                ValidFileTypes = new[] { PluginFinderOptions.DefaultPluginFileType },
            };
            var pluginFinder = new PluginFinder<IPlugin>(finderOptions);
            var pluginFinderResults = pluginFinder.FindAssemliesWithPlugins();

            if (!(pluginFinderResults?.Any() ?? false))
            {
                // Failed to find any eligible plugins to load
                return services;
            }

            // Replace default Razor view compiler with custom one to help locate Mvc Views in
            // plugins folder. Faster than using RazorViewEngineOptions.ViewLocationExpanders
            services.Replace<IViewCompilerProvider, PluginViewCompilerProvider>();

            var mvcBuilder = services
                .AddControllersWithViews()
                .AddRazorRuntimeCompilation();

            // Load all valid plugin assemblies found in their own AssemblyLoadContext 
            var pluginAssemblies = pluginFinder.LoadPluginAssemblies(pluginFinderResults);
            foreach (var result in pluginAssemblies)
            {
                // TODO: New service collection for each plugin?
                // TODO: Register sharedServiceHosts with new service collection if so
                //var serviceCollection = new ServiceCollection();
                if (result.Assembly == null)
                {
                    _logger.LogError($"Failed to load assembly for plugin '{result.AssemblyPath}', skipping.");
                    continue;
                }

                // Load and activate plugins found by plugin finder
                var pluginLoader = new PluginLoader<IPlugin>(result, Options.SharedServiceHosts, services);
                var loadedPlugins = pluginLoader.LoadedPlugins;
                if (!loadedPlugins.Any())
                {
                    _logger.LogError($"Failed to find any valid plugins in assembly '{result.AssemblyPath}'");
                    continue;
                }

                // Register assembly as application part with Mvc
                mvcBuilder.AddApplicationPart(result.Assembly);

                // Loop through all loaded plugins and register plugin services and register plugins
                await InitLoadedPluginsAsync(loadedPlugins, result.Assembly, mvcBuilder);
            }
            return services;
        }

        public async Task LoadPluginAsync(string filePath)
        {
            var rootPluginsDirectory = Options.RootPluginsDirectory;
            var finderOptions = new PluginFinderOptions
            {
                PluginType = typeof(IPlugin),
                RootPluginsDirectory = rootPluginsDirectory,
                ValidFileTypes = new[] { PluginFinderOptions.DefaultPluginFileType },
            };
            var pluginFinder = new PluginFinder<IPlugin>(finderOptions);
            var pluginFinderResults = pluginFinder.FindPluginInAssembly(filePath);

            var mvcBuilder = _services
                .AddControllersWithViews()
                .AddRazorRuntimeCompilation();

            // Load all valid plugin assemblies found in their own AssemblyLoadContext 
            var pluginAssemblies = pluginFinder.LoadPluginAssemblies(pluginFinderResults);
            foreach (var result in pluginAssemblies)
            {
                if (result.Assembly == null)
                {
                    _logger.LogError($"Failed to load assembly for plugin '{result.AssemblyPath}', skipping.");
                    continue;
                }

                // Load and activate plugins found by plugin finder
                var pluginLoader = new PluginLoader<IPlugin>(result, Options.SharedServiceHosts, _services);
                var loadedPlugins = pluginLoader.LoadedPlugins;
                if (!loadedPlugins.Any())
                {
                    _logger.LogError($"Failed to find any valid plugins in assembly '{result.AssemblyPath}'");
                    continue;
                }

                await InitLoadedPluginsAsync(loadedPlugins, result.Assembly, mvcBuilder);
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
                    _logger.LogWarning($"[{plugin.Name}] Existing plugin version is newer than incoming plugin version, skipping registration...");
                    return;
                }
                else if (oldVersion < newVersion)
                {
                    // Incoming is newer
                    _logger.LogWarning($"[{plugin.Name}] Existing plugin version is newer than incoming plugin version, removing old version and adding new version...");

                    // Remove existing plugin so we can add newer version of plugin
                    await RemoveAsync(plugin.Name);
                }
                else
                {
                    // Plugin versions are the same
                    _logger.LogWarning($"[{plugin.Name}] Existing plugin version is the same version as incoming plugin version, skipping registration...");
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
                _logger.LogWarning($"[{pluginName}] Unable to stop plugin, no plugin with that name is currently loaded or registered");
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
                _logger.LogWarning($"[{pluginName}] Unable to reload plugin, no plugin with that name is currently loaded or registered");
                return;
            }

            var pluginHost = _plugins[pluginName];
            var previousState = pluginHost.State;

            // Remove plugin from cache
            await RemoveAsync(pluginName, unload: false);

            // Reload plugin context by unloading and loading it again
            pluginHost.Reload();

            // Call plugin 'OnReload' callback handler
            pluginHost.Plugin.OnReload();

            // Set plugin state to running
            pluginHost.SetState(PluginState.Running);

            // Inform host application that the plugin state has changed by
            // invoking the 'StateChanged' event
            OnPluginHostStateChanged(pluginHost, previousState);
            //await SaveStateAsync(pluginName, PluginState.Running);

            _plugins[pluginName] = pluginHost;

            // Call plugin's load method
            pluginHost.Plugin.OnLoad();

            // Register plugin host with plugin manager
            await RegisterPluginAsync((PluginHost)pluginHost);

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

        public async Task RemoveAsync(string pluginName, bool unload = true)
        {
            if (!_plugins.ContainsKey(pluginName))
            {
                _logger.LogWarning($"[{pluginName}] Unable to remove plugin, no plugin with that name is currently loaded or registered");
                return;
            }

            var pluginHost = _plugins[pluginName];
            var previousState = pluginHost.State;
            if (unload)
            {
                pluginHost.Unload();
            }

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
                _logger.LogError($"[{pluginName}] Failed to set plugin state, plugin does not exist in cache");
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

        #endregion

        #region Private Methods

        private async Task InitLoadedPluginsAsync(IEnumerable<PluginHost> pluginHosts, Assembly assembly, IMvcBuilder mvcBuilder)
        {
            // Loop through all loaded plugins and register plugin services and register plugins
            foreach (var pluginHost in pluginHosts)
            {
                var pluginType = pluginHost.Plugin.GetType();

                // Check if plugin is marked with 'StaticFilesLocation' attribute
                pluginType.RegisterStaticFiles(_webHostEnv, assembly);

                // Register any PluginServices found with IServiceCollection
                _services.RegisterPluginServices(pluginHost.PluginServices);

                // Call plugin's ConfigureServices method to register any services
                pluginHost.Plugin.ConfigureServices(_services);

                // Call plugin's 'ConfigureMvcBuilder' method to allow configuring Mvc
                pluginHost.Plugin.ConfigureMvcBuilder(mvcBuilder);

                // Call plugin's load method
                pluginHost.Plugin.OnLoad();

                // Register plugin host with plugin manager
                await RegisterPluginAsync(pluginHost);
            }
        }

        #endregion
    }
}