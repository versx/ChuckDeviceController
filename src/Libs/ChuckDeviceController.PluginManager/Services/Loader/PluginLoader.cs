﻿namespace ChuckDeviceController.PluginManager.Services.Loader
{
    using System.Reflection;

    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;

    using ChuckDeviceController.Common.Data;
    using ChuckDeviceController.Plugin;
    using ChuckDeviceController.Plugin.Services;
    using ChuckDeviceController.PluginManager.Extensions;
    using ChuckDeviceController.PluginManager.Mvc.Extensions;
    using ChuckDeviceController.PluginManager.Services.Finder;

    public class PluginLoader<TPlugin> : IPluginLoader
    {
        private static readonly ILogger<IPluginLoader> _logger =
            new Logger<IPluginLoader>(LoggerFactory.Create(x => x.AddConsole()));

        public IEnumerable<PluginHost> LoadedPlugins { get; }

        public event EventHandler<PluginLoadedEventArgs>? PluginLoaded;
        private void OnPluginLoaded(IPluginHost pluginHost)
        {
            PluginLoaded?.Invoke(this, new PluginLoadedEventArgs(pluginHost));
        }

        public PluginLoader(PluginFinderResult<IPlugin> pluginResult, IReadOnlyDictionary<Type, object> sharedServiceHosts, IServiceCollection services)
        {
            if (pluginResult.Assembly == null)
            {
                throw new NullReferenceException($"Failed to load plugin assembly '{pluginResult.AssemblyPath}'");
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

                /*
                foreach (var pluginBootstrapper in pluginBootstrapTypes)
                {
                    var bootstrapAttributes = pluginBootstrapper.GetCustomAttributes<PluginBootstrapperAttribute>(sharedServiceHosts);
                    if (bootstrapAttributes != null)
                    {
                        foreach (var attr in bootstrapAttributes)
                        {
                            //attr.PluginType
                            if (attr.PluginType == pluginType)
                            {
                                var bootstrapMethod = pluginBootstrapper.GetMethod(nameof(IPluginBootstrapper.Bootstrap));
                                if (bootstrapMethod != null)
                                {
                                    var instance = bootstrapMethod.Invoke(pluginType, new[] { services });
                                    Console.WriteLine($"Method: {instance}");
                                }
                            }
                        }
                    }
                }
                */

                // Initialize any fields or properties marked as plugin service types
                var typeInfo = pluginInstance.GetType().GetTypeInfo();
                typeInfo.SetPluginServiceFields(pluginInstance, sharedServiceHosts);
                typeInfo.SetPluginServiceProperties(pluginInstance, sharedServiceHosts);

                var permissions = pluginType.GetPluginPermissions();
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