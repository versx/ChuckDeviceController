﻿namespace ChuckDeviceConfigurator.Services.Plugins
{
    using ChuckDeviceController.Plugins;

    /// <summary>
    /// Wrapper for loaded plugins
    /// </summary>
    public interface IPluginHost
    {
        /// <summary>
        /// Gets the instantiated <seealso cref="IPlugin"/> type
        /// for the loaded plugin.
        /// </summary>
        IPlugin Plugin { get; }

        /// <summary>
        /// Gets the requested <seealso cref="PluginPermissions"/>
        /// for the loaded plugin.
        /// </summary>
        PluginPermissions Permissions { get; }

        /// <summary>
        /// Gets the state of the plugin, whether it's running,
        /// stopped, or in an error state.
        /// </summary>
        PluginState State { get; }

        /// <summary>
        /// Gets a value determining whether the plugin is enabled
        /// or not.
        /// </summary>
        bool IsEnabled { get; }

        /// <summary>
        /// Gets the <seealso cref="PluginEventHandlers"/> for the
        /// loaded plugin that are called when events occur for the
        /// related plugin event type.
        /// </summary>
        PluginEventHandlers EventHandlers { get; }
    }
}