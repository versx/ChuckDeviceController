namespace ChuckDeviceConfigurator.Services.Plugins
{
    using System.ComponentModel;

    using ChuckDeviceController.Plugins;

    public sealed class PluginHost : IPluginHost
    {
        #region Properties

        public IPlugin Plugin { get; }

        // TODO: Change 'Permissions' to 'RequestedPermissions' and add 'AllowedPermissions' property
        public PluginPermissions Permissions { get; } = PluginPermissions.None;

        public PluginState State { get; private set; }

        [DisplayName("Enabled")]
        public bool IsEnabled { get; private set; }

        public PluginEventHandlers EventHandlers { get; } = new();

        #endregion

        #region Constructors

        public PluginHost(IPlugin plugin, PluginPermissions permissions)
            : this(plugin, permissions, new())
        {
        }

        public PluginHost(IPlugin plugin, PluginPermissions permissions, PluginEventHandlers handlers)
        {
            Plugin = plugin;
            Permissions = permissions;
            EventHandlers = handlers;
            State = PluginState.Unset;
        }

        #endregion

        #region Public Methods

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