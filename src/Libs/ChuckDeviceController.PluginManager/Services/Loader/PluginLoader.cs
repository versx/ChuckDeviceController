namespace ChuckDeviceController.PluginManager.Services.Loader
{
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;

    using ChuckDeviceController.Common.Data;
    using ChuckDeviceController.Plugin;
    using ChuckDeviceController.PluginManager.Extensions;
    using ChuckDeviceController.PluginManager.Mvc.Extensions;
    using ChuckDeviceController.PluginManager.Services.Finder;

    public class PluginLoader<TPlugin> : IPluginLoader
    {
        // Variables
        private static readonly ILogger<IPluginLoader> _logger =
            new Logger<IPluginLoader>(LoggerFactory.Create(x => x.AddConsole()));


        // Properties
        public IEnumerable<PluginHost> LoadedPlugins { get; }

        // Events
        public event EventHandler<PluginLoadedEventArgs>? PluginLoaded;
        private void OnPluginLoaded(IPluginHost pluginHost)
        {
            PluginLoaded?.Invoke(this, new PluginLoadedEventArgs(pluginHost));
        }

        // Constructors
        public PluginLoader(
            PluginFinderResult<IPlugin> pluginResult,
            IReadOnlyDictionary<Type, object> sharedServiceHosts,
            IServiceCollection services)
        {
            if (pluginResult.Assembly == null)
            {
                throw new NullReferenceException($"Failed to load plugin assembly '{pluginResult.AssemblyPath}'");
            }

            var assemblyTypes = pluginResult.Assembly.GetTypes();
            //var pluginBootstrapTypes = assemblyTypes.GetAssignableTypes<IPluginBootstrapper>();
            var pluginTypes = assemblyTypes.GetAssignableTypes<IPlugin>();

            var pluginServiceDescriptors = new List<ServiceDescriptor>();
            var loadedPlugins = new List<PluginHost>();

            // Register all marked service classes with 'PluginServiceAttribute'
            foreach (var type in assemblyTypes)
            {
                // Find service classes with 'PluginServiceAttribute'
                var pluginServices = type.GetPluginServicesWithAttribute(sharedServiceHosts);
                if (pluginServices == null)
                    continue;

                pluginServiceDescriptors.AddRange(pluginServices);
            }

            // Loop all found plugin types and create/instantiate instances of them
            foreach (var pluginType in pluginTypes)
            {
                // TODO: var test = services.GetParameterInstances(pluginType);

                // Instantiate an instance of the plugin type
                var pluginInstance = pluginType.CreatePluginInstance(sharedServiceHosts);
                if (pluginInstance == null)
                {
                    _logger.LogError($"Failed to instantiate plugin type instance '{pluginType.Name}'");
                    continue;
                }

                if (pluginInstance is not IPlugin plugin)
                {
                    _logger.LogError($"Failed to instantiate '{nameof(IPlugin)}' instance");
                    continue;
                }

                // Initialize any fields or properties marked as plugin service types
                pluginInstance.SetPluginServiceFields(sharedServiceHosts);
                pluginInstance.SetPluginServiceProperties(sharedServiceHosts);

                var requestedPermissions = pluginType.GetPluginPermissions();
                // TODO: Determine allowed permissions based on accepted permissions policy
                var allowedPermissions = PluginPermissions.None;
                var permissionsOptions = new PluginPermissionsOptions
                (
                    requestedPermissions: requestedPermissions,
                    allowedPermissions: allowedPermissions,
                    // TODO: Make 'PluginAcceptedPermissionsPolicy' configurable via UI (for all plugins)
                    acceptedPermissionsPolicy: PluginAcceptedPermissionsPolicy.AcceptAllAutomatically
                );

                var pluginHost = new PluginHost(
                    plugin,
                    permissionsOptions,
                    new PluginAssembly(pluginResult.Assembly),
                    pluginResult.LoadContext,
                    pluginServiceDescriptors,
                    new PluginEventHandlers(),
                    PluginState.Running
                );

                // Set inherited plugin event callbacks
                foreach (var type in pluginType.GetInterfaces())
                {
                    if (typeof(IUiEvents) == type)
                        pluginHost.EventHandlers.UiEvents = (IUiEvents)plugin;
                    else if (typeof(IDatabaseEvents) == type)
                        pluginHost.EventHandlers.DatabaseEvents = (IDatabaseEvents)plugin;
                    else if (typeof(IJobControllerServiceEvents) == type)
                        pluginHost.EventHandlers.JobControllerEvents = (IJobControllerServiceEvents)plugin;
                    else if (typeof(ISettingsPropertyEvents) == type)
                        pluginHost.EventHandlers.SettingsEvents = (ISettingsPropertyEvents)plugin;
                }

                loadedPlugins.Add(pluginHost);

                // Call PluginLoaded event
                OnPluginLoaded(pluginHost);
            }

            LoadedPlugins = loadedPlugins;
        }
    }
}