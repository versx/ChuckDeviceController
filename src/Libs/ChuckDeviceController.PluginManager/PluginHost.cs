namespace ChuckDeviceController.PluginManager
{
    using Microsoft.Extensions.DependencyInjection;

    using ChuckDeviceController.Common.Data;
    using ChuckDeviceController.Plugin;
    using ChuckDeviceController.PluginManager.Extensions;
    using ChuckDeviceController.PluginManager.Services.Finder;
    using ChuckDeviceController.PluginManager.Services.Loader;

    public sealed class PluginHost : IPluginHost
    {
        #region Properties

        public IPlugin Plugin { get; }

        public PluginPermissionsOptions PermissionsOptions { get; }

        public PluginState State { get; private set; }

        public IAssemblyShim Assembly { get; }

        public IPluginAssemblyLoadContext LoadContext { get; private set; }

        public IEnumerable<ServiceDescriptor> PluginServices { get; }

        public PluginEventHandlers EventHandlers { get; } = new();

        #endregion

        #region Constructors

        public PluginHost(IPlugin plugin,
            PluginPermissionsOptions permissionOptions,
            IAssemblyShim assembly,
            IPluginAssemblyLoadContext loadContext,
            IEnumerable<ServiceDescriptor> pluginServices,
            PluginEventHandlers eventHandlers,
            PluginState state = PluginState.Unset)
        {
            Plugin = plugin;
            PermissionsOptions = permissionOptions;
            LoadContext = loadContext;
            Assembly = assembly;
            PluginServices = pluginServices;
            EventHandlers = eventHandlers;
            State = state;

            if (permissionOptions.AcceptedPermissionsPolicy == PluginAcceptedPermissionsPolicy.AcceptAllAutomatically)
            {
                AcceptPermissions(PermissionsOptions.RequestedPermissions);
            }
        }

        #endregion

        #region Public Methods

        public void Reload()
        {
            LoadContext.Unload();

            var entryAssembly = System.Reflection.Assembly.GetEntryAssembly();
            var hostFramework = entryAssembly?.GetHostFramework();
            LoadContext = PluginAssemblyLoadContext.Create<IPlugin>(Assembly.AssemblyFullPath, hostFramework);
        }

        public void Unload()
        {
            LoadContext.Unload();
        }

        public void SetState(PluginState state, bool ignoreEvent = false)
        {
            var test = PermissionsOptions.AllowedPermissions |= PluginPermissions.WriteDatabase;
            State = state;

            if (!ignoreEvent)
            {
                // Call 'OnStateChanged' event handler for plugin
                Plugin.OnStateChanged(state);
            }
        }

        public void AcceptPermissions(PluginPermissions acceptedPermissions)
        {
            PermissionsOptions.Accept(acceptedPermissions);
        }

        public void RevokePermissions(PluginPermissions revokePermissions)
        {
            PermissionsOptions.Revoke(revokePermissions);
        }

        #endregion
    }

    public class PluginPermissionsOptions
    {
        public PluginPermissions RequestedPermissions { get; }

        public PluginPermissions AllowedPermissions { get; internal set; }

        public PluginAcceptedPermissionsPolicy AcceptedPermissionsPolicy { get; }

        public PluginPermissionsOptions(
            PluginPermissions requestedPermissions,
            PluginPermissions allowedPermissions,
            PluginAcceptedPermissionsPolicy acceptedPermissionsPolicy)
        {
            RequestedPermissions = requestedPermissions;
            AllowedPermissions = allowedPermissions;
            AcceptedPermissionsPolicy = acceptedPermissionsPolicy;
        }

        public bool IsAllowed(PluginPermissions permissions)
        {
            return (AllowedPermissions & permissions) == permissions;
        }

        public void Accept(PluginPermissions permissions)
        {
            AllowedPermissions |= permissions;
        }

        public void Revoke(PluginPermissions permissions)
        {
            AllowedPermissions &= (~permissions);
        }
    }

    public interface IPluginPermissionsEvents
    {
        void OnPermissionGranted(PluginPermissions permissions);

        void OnPermissionDenied(PluginPermissions permissions);
    }

    public enum PluginAcceptedPermissionsPolicy
    {
        AcceptManually = 0, // AcceptNone?
        AcceptSpecific,
        AcceptAllAutomatically,
    }
}