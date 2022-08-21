namespace ChuckDeviceController.Plugin
{
    /// <summary>
    /// Defines which permissions the plugin is going to request
    /// in order to operate correctly.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class PluginPermissionsAttribute : Attribute
    {
        /// <summary>
        /// Gets the requested permissions of the plugin.
        /// </summary>
        public PluginPermissions Permissions { get; }

        /// <summary>
        /// Instantiates a new plugin permissions attribute.
        /// </summary>
        /// <param name="permissions">Plugin permissions to request upon load.</param>
        public PluginPermissionsAttribute(PluginPermissions permissions)
        {
            Permissions = permissions;
        }
    }
}