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

        public Type PluginType { get; }

        // TODO: Change 'Permissions' to 'RequestedPermissions' and add 'AllowedPermissions' property
        public PluginPermissions Permissions { get; } = PluginPermissions.None;

        public PluginState State { get; private set; }

        public PluginFinderResult PluginFinderResult { get; }

        public PluginEventHandlers EventHandlers { get; } = new();

        public IReadOnlyDictionary<string, IJobController> JobControllers => _jobControllers;

        #endregion

        #region Constructors

        public PluginHost(IPlugin plugin, PluginPermissions permissions, PluginFinderResult result)
            : this(plugin, permissions, result, new())
        {
        }

        public PluginHost(IPlugin plugin, PluginPermissions permissions, PluginFinderResult result, PluginEventHandlers eventHandlers, PluginState state = PluginState.Unset)
        {
            Plugin = plugin;
            PluginType = plugin.GetType();
            Permissions = permissions;
            PluginFinderResult = result;
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