namespace ChuckDeviceConfigurator.Services.Plugins
{
    using System.Reflection;

    using ChuckDeviceConfigurator.Services.Plugins.Extensions;
    using ChuckDeviceController.Common.Data;
    using ChuckDeviceController.Plugins;
    using ChuckDeviceController.Plugins.Services;

    public class PluginLoader<TPlugin> : IPluginLoader<TPlugin> where TPlugin : class, IPlugin
    {
        private static readonly ILogger<IPluginLoader<TPlugin>> _logger =
            new Logger<IPluginLoader<TPlugin>>(LoggerFactory.Create(x => x.AddConsole()));

        public IEnumerable<PluginHost> LoadedPlugins { get; }

        public PluginLoader(PluginFinderResult pluginResult, IReadOnlyDictionary<Type, object> sharedServiceHosts)
        {
            if (pluginResult.Assembly == null)
            {
                throw new NullReferenceException($"Failed to load plugin assembly: '{pluginResult.FullAssemblyPath}'");
            }

            var assemblyTypes = pluginResult.Assembly.GetTypes();
            var pluginBootstrapTypes = assemblyTypes.GetAssignableTypes<IPluginBootstrapper>();
            var pluginTypes = assemblyTypes.GetAssignableTypes<IPlugin>();

            var pluginServiceDescriptors = new List<ServiceDescriptor>();
            var loadedPlugins = new List<PluginHost>();

            // Register all marked service classes with 'PluginServiceAttribute'
            foreach (var type in assemblyTypes)
            {
                // TODO: Add support for HostedServices

                // Find service classes with 'PluginServiceAttribute'
                var pluginService = type.GetPluginServiceWithAttribute();
                if (pluginService == null)
                    continue;

                pluginServiceDescriptors.Add(pluginService);
            }

            // Loop all found plugin types and create/instantiate instances of them
            foreach (var pluginType in pluginTypes)
            {
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
                var typeInfo = pluginInstance.GetType().GetTypeInfo();
                typeInfo.SetPluginServiceFields(pluginInstance, sharedServiceHosts);
                typeInfo.SetPluginServiceProperties(pluginInstance, sharedServiceHosts);

                var permissions = pluginType.GetPermissions();
                var pluginHost = new PluginHost(
                    plugin,
                    permissions,
                    pluginResult,
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
                    else if (typeof(IJobControllerServiceHost) == type)
                        pluginHost.EventHandlers.JobControllerEvents = (IJobControllerServiceEvents)plugin;
                }

                loadedPlugins.Add(pluginHost);
            }

            LoadedPlugins = loadedPlugins;
        }
    }
}