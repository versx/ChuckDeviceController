namespace ChuckDeviceController.Plugin;

/// <summary>
/// Determines the location of any static files and folders.
/// i.e. 'wwwroot'
/// </summary>
public enum StaticFilesLocation
{
    /// <summary>
    /// No static files or folders from the plugin.
    /// </summary>
    None = 0,

    /// <summary>
    /// Static files are embedded in a resource file
    /// within the plugin.
    /// </summary>
    Resources,

    /// <summary>
    /// Static files are located externally on the
    /// file system.
    /// </summary>
    External,
}