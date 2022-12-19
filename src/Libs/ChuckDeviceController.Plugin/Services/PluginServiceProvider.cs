namespace ChuckDeviceController.Plugin.Services;

/// <summary>
/// Determines who provided the plugin service to register with
/// dependency injection.
/// </summary>
public enum PluginServiceProvider
{
    /// <summary>
    /// Service was provided by the plugin.
    /// </summary>
    Plugin = 0,

    /// <summary>
    /// Service was provided by the host application.
    /// </summary>
    Host,
}