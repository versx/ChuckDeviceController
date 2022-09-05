namespace ChuckDeviceController.PluginManager
{
    using Microsoft.Extensions.DependencyInjection;

    using ChuckDeviceController.Common.Data;
    using ChuckDeviceController.Plugin;
    using ChuckDeviceController.PluginManager.Extensions;
    using ChuckDeviceController.PluginManager.Services.Finder;
    using ChuckDeviceController.PluginManager.Services.Loader;

    public sealed class PluginHost : IPluginHost
    {
        #region Properties

        public IPlugin Plugin { get; }

        // TODO: Change 'Permissions' to 'RequestedPermissions' and add 'AllowedPermissions' property
        public PluginPermissions Permissions { get; } = PluginPermissions.None;

        public PluginState State { get; private set; }

        public IAssemblyShim Assembly { get; }

        public IPluginAssemblyLoadContext LoadContext { get; private set; }

        public IEnumerable<ServiceDescriptor> PluginServices { get; }

        public PluginEventHandlers EventHandlers { get; } = new();

        #endregion

        #region Constructors

        public PluginHost(
            IPlugin plugin,
            PluginPermissions permissions,
            IAssemblyShim assembly,
            IPluginAssemblyLoadContext loadContext,
            IEnumerable<ServiceDescriptor> pluginServices)
            : this(plugin, permissions, assembly, loadContext, pluginServices, new())
        {
        }

        public PluginHost(IPlugin plugin,
            PluginPermissions permissions,
            IAssemblyShim assembly,
            IPluginAssemblyLoadContext loadContext,
            IEnumerable<ServiceDescriptor> pluginServices,
            PluginEventHandlers eventHandlers,
            PluginState state = PluginState.Unset)
        {
            Plugin = plugin;
            Permissions = permissions;
            LoadContext = loadContext;
            Assembly = assembly;
            PluginServices = pluginServices;
            EventHandlers = eventHandlers;
            State = state;
        }

        #endregion

        #region Public Methods

        public void Reload()
        {
            LoadContext.Unload();

            var entryAssembly = System.Reflection.Assembly.GetEntryAssembly();
            var hostFramework = entryAssembly?.GetHostFramework();
            LoadContext = PluginAssemblyLoadContext.Create<IPlugin>(Assembly.AssemblyFullPath, hostFramework);
        }

        public void Unload()
        {
            LoadContext.Unload();
        }

        public void SetState(PluginState state, bool ignoreEvent = false)
        {
            State = state;

            if (!ignoreEvent)
            {
                // Call 'OnStateChanged' event handler for plugin
                Plugin.OnStateChanged(state);
            }
        }

        #endregion
    }
}