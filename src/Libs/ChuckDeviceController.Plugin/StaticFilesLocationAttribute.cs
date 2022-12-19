namespace ChuckDeviceController.Plugin;

/// <summary>
/// Determines where the static files (i.e. 'wwwroot' and 'Views') will be located
/// relevant to the plugin.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class StaticFilesLocationAttribute : Attribute
{
    /// <summary>
    /// Gets an enum value determining where any Mvc Views are located.
    /// i.e. 'Views' folder.
    /// </summary>
    public StaticFilesLocation Views { get; } = StaticFilesLocation.None;

    /// <summary>
    /// Gets an enum value determining where any web resource files are
    /// located. i.e. 'wwwroot' web root folder.
    /// </summary>
    public StaticFilesLocation WebRoot { get; } = StaticFilesLocation.None;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="views"></param>
    /// <param name="webRoot"></param>
    public StaticFilesLocationAttribute(
        StaticFilesLocation views = StaticFilesLocation.None,
        StaticFilesLocation webRoot = StaticFilesLocation.None)
    {
        Views = views;
        WebRoot = webRoot;
    }
}