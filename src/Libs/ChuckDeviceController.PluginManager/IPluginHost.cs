namespace ChuckDeviceController.PluginManager
{
    using System.ComponentModel;

    using Microsoft.Extensions.DependencyInjection;

    using ChuckDeviceController.Common.Data;
    using ChuckDeviceController.Plugin;
    using ChuckDeviceController.PluginManager.Services.Finder;
    using ChuckDeviceController.PluginManager.Services.Loader;

    /// <summary>
    /// Wrapper for loaded plugins
    /// </summary>
    public interface IPluginHost
    {
        #region Properties

        /// <summary>
        /// Gets the instantiated <seealso cref="IPlugin"/> type for the
        /// loaded plugin.
        /// </summary>
        IPlugin Plugin { get; }

        /// <summary>
        /// Gets the requested <seealso cref="PluginPermissions"/>
        /// for the loaded plugin.
        /// </summary>
        [DisplayName("Permissions Options")]
        PluginPermissionsOptions PermissionsOptions { get; }
        //PluginPermissions RequestedPermissions { get; }
        //PluginPermissions AllowedPermissions { get; }

        /// <summary>
        /// Gets the state of the plugin, whether it is enabled and running,
        /// stopped, disabled, in an error state, etc.
        /// </summary>
        PluginState State { get; }

        /// <summary>
        /// Gets the <seealso cref="IAssemblyShim"/> of the loaded plugin
        /// assembly.
        /// </summary>
        IAssemblyShim Assembly { get; }

        /// <summary>
        /// Gets or sets the plugin assembly's loading context (ALC) which
        /// will contain the assembly and any dependants or references that
        /// have been loaded in the context.
        /// </summary>
        IPluginAssemblyLoadContext LoadContext { get; }

        /// <summary>
        /// Gets a list of service descriptors for the plugin that have been
        /// decorated in the plugin with the 'PluginService' attribute. These
        /// plugin services will be registered with dependency injection service
        /// in the host application.
        /// </summary>
        IEnumerable<ServiceDescriptor> PluginServices { get; }

        /// <summary>
        /// Gets the <seealso cref="PluginEventHandlers"/> for the
        /// loaded plugin that are called when events occur for the
        /// related plugin event type.
        /// </summary>
        PluginEventHandlers EventHandlers { get; }

        #endregion

        #region Methods

        /// <summary>
        /// Sets the current state of the plugin.
        /// </summary>
        /// <param name="state">Plugin state to set.</param>
        /// <param name="ignoreEvent"></param>
        void SetState(PluginState state, bool ignoreEvent = false);

        /// <summary>
        /// Unloads the plugin assembly from the <seealso cref="IPluginAssemblyLoadContext"/>
        /// then loads it again.
        /// </summary>
        void Reload();

        /// <summary>
        /// Unloads the plugin assembly from the <seealso cref="IPluginAssemblyLoadContext"/>
        /// which effectively stops the plugin and is no longer running or loaded.
        /// </summary>
        void Unload();

        #endregion
    }
}