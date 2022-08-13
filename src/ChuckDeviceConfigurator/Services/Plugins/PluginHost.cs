namespace ChuckDeviceConfigurator.Services.Plugins
{
    using System.ComponentModel;

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

        [DisplayName("Enabled")]
        public bool IsEnabled { get; private set; }

        public PluginEventHandlers EventHandlers { get; } = new();

        public IReadOnlyDictionary<string, IJobController> JobControllers => _jobControllers;

        #endregion

        #region Constructors

        public PluginHost(IPlugin plugin, PluginPermissions permissions)
            : this(plugin, permissions, new())
        {
        }

        public PluginHost(IPlugin plugin, PluginPermissions permissions, PluginEventHandlers handlers, PluginState state = PluginState.Unset)
        {
            Plugin = plugin;
            Permissions = permissions;
            EventHandlers = handlers;
            State = state;
        }

        #endregion

        #region Public Methods

        public void AddJobController(string name, IJobController jobController)
        {
            // TODO: JobControllers cache created by plugin
        }

        public void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;
        }

        public void SetState(PluginState state)
        {
            State = state;

            // Call 'OnStateChanged' event handler for plugin
            Plugin.OnStateChanged(state, IsEnabled);
        }

        #endregion
    }
}