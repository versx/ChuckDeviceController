namespace ChuckDeviceController.Plugin;

/// <summary>
///     Defines where the static files or folders (i.e. `wwwroot` and `Views`) will
///     be located, relevant to the plugin's path.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class StaticFilesLocationAttribute : Attribute
{
    /// <summary>
    ///     Gets an enum value defining where the plugin's Mvc Views folder is located.
    ///     i.e. `Views` folder.
    /// </summary>
    public StaticFilesLocation Views { get; } = StaticFilesLocation.None;

    /// <summary>
    ///     Gets an enum value determining where any web resource files are
    ///     located. i.e. `wwwroot` web root folder.
    /// </summary>
    public StaticFilesLocation WebRoot { get; } = StaticFilesLocation.None;

    /// <summary>
    ///     Instantiates a new instance of the <see cref="StaticFilesLocationAttribute"/>
    ///     attribute class.
    /// </summary>
    /// <param name="views">
    ///     Determines where the Mvc `Views` folder is located.
    /// </param>
    /// <param name="webRoot">
    ///     Determines where the `wwwroot` folder is located.
    /// </param>
    public StaticFilesLocationAttribute(
        StaticFilesLocation views = StaticFilesLocation.None,
        StaticFilesLocation webRoot = StaticFilesLocation.None)
    {
        Views = views;
        WebRoot = webRoot;
    }
}