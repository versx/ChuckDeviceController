namespace ChuckDeviceController.PluginManager
{
    using Microsoft.Extensions.DependencyInjection;

    using ChuckDeviceController.Common.Data;
    using ChuckDeviceController.Common.Jobs;
    using ChuckDeviceController.PluginManager.Services.Finder;
    using ChuckDeviceController.Plugins;

    public sealed class PluginHost : IPluginHost
    {
        private readonly Dictionary<string, IJobController> _jobControllers = new();

        #region Properties

        public IPlugin Plugin { get; }

        // TODO: Change 'Permissions' to 'RequestedPermissions' and add 'AllowedPermissions' property
        public PluginPermissions Permissions { get; } = PluginPermissions.None;

        public PluginState State { get; private set; }

        public PluginFinderResult<IPlugin> PluginFinderResult { get; }

        public IEnumerable<ServiceDescriptor> PluginServices { get; }

        public PluginEventHandlers EventHandlers { get; } = new();

        public IReadOnlyDictionary<string, IJobController> JobControllers => _jobControllers;

        #endregion

        #region Constructors

        public PluginHost(
            IPlugin plugin,
            PluginPermissions permissions,
            PluginFinderResult<IPlugin> result,
            IEnumerable<ServiceDescriptor> pluginServices)
            : this(plugin, permissions, result, pluginServices, new())
        {
        }

        public PluginHost(IPlugin plugin,
            PluginPermissions permissions,
            PluginFinderResult<IPlugin> result,
            IEnumerable<ServiceDescriptor> pluginServices,
            PluginEventHandlers eventHandlers,
            PluginState state = PluginState.Unset)
        {
            Plugin = plugin;
            Permissions = permissions;
            PluginFinderResult = result;
            PluginServices = pluginServices;
            EventHandlers = eventHandlers;
            State = state;
        }

        #endregion

        #region Public Methods

        public void Unload()
        {
            PluginFinderResult.AssemblyLoadContext.Unload();
        }

        public void AddJobController(string name, IJobController jobController)
        {
            // TODO: JobControllers cache created by plugin
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