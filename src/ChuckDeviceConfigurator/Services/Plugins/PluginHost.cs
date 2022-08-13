namespace ChuckDeviceConfigurator.Services.Plugins
{
    using ChuckDeviceController.Common.Data;
    using ChuckDeviceController.Common.Jobs;
    using ChuckDeviceController.Plugins;

    public sealed class PluginHost : IPluginHost
    {
        private readonly Dictionary<string, IJobController> _jobControllers = new();

        #region Properties

        public IPlugin Plugin { get; }

        // TODO: Change 'Permissions' to 'RequestedPermissions' and add 'AllowedPermissions' property
        public PluginPermissions Permissions { get; } = PluginPermissions.None;

        public PluginState State { get; private set; }

        public PluginEventHandlers EventHandlers { get; } = new();

        public IReadOnlyDictionary<string, IJobController> JobControllers => _jobControllers;

        // TODO: Cache plugin file path for reloading

        #endregion

        #region Constructors

        public PluginHost(IPlugin plugin, PluginPermissions permissions)
            : this(plugin, permissions, new())
        {
        }

        public PluginHost(IPlugin plugin, PluginPermissions permissions, PluginEventHandlers eventHandlers, PluginState state = PluginState.Unset)
        {
            Plugin = plugin;
            Permissions = permissions;
            EventHandlers = eventHandlers;
            State = state;
        }

        #endregion

        #region Public Methods

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