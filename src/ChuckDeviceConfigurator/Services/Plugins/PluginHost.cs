namespace ChuckDeviceConfigurator.Services.Plugins
{
    using ChuckDeviceController.Plugins;

    public sealed class PluginHost : IPluginHost
    {
        #region Properties

        public IPlugin Plugin { get; }

        // TODO: Change 'Permissions' to 'RequestedPermissions' and add 'AllowedPermissions' property
        public PluginPermissions Permissions { get; } = PluginPermissions.None;

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
        }

        #endregion
    }
}