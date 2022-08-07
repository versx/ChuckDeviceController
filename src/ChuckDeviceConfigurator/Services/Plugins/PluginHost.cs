namespace ChuckDeviceConfigurator.Services.Plugins
{
    using ChuckDeviceController.Plugins;

    public class PluginHost : IPluginHost
    {
        public IPlugin Plugin { get; }

        public PluginPermissions Permissions { get; }

        public PluginEventHandlers EventHandlers { get; }

        public PluginHost()
        {
            EventHandlers = new();
        }

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
    }
}