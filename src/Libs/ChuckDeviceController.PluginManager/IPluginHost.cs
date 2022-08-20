﻿namespace ChuckDeviceController.PluginManager
{
    using Microsoft.Extensions.DependencyInjection;

    using ChuckDeviceController.Common.Data;
    using ChuckDeviceController.Plugin;
    using ChuckDeviceController.PluginManager.Services.Finder;

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
        /// Gets the state of the plugin, whether it is enabled and running,
        /// stopped, disabled, in an error state, etc.
        /// </summary>
        PluginState State { get; }

        /// <summary>
        /// 
        /// </summary>
        PluginFinderResult<IPlugin> PluginFinderResult { get; }

        /// <summary>
        /// 
        /// </summary>
        IEnumerable<ServiceDescriptor> PluginServices { get; }

        /// <summary>
        /// Gets the <seealso cref="PluginEventHandlers"/> for the
        /// loaded plugin that are called when events occur for the
        /// related plugin event type.
        /// </summary>
        PluginEventHandlers EventHandlers { get; }

        /// <summary>
        /// Sets the state of the plugin.
        /// </summary>
        /// <param name="state">Plugin state to set.</param>
        /// <param name="ignoreEvent"></param>
        void SetState(PluginState state, bool ignoreEvent = false);

        /// <summary>
        /// 
        /// </summary>
        void Unload();
    }
}