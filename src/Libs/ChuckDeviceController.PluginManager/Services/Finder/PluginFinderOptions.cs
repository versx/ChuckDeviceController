namespace ChuckDeviceController.PluginManager.Services.Finder;

public class PluginFinderOptions : IPluginFinderOptions
{
    public const string DefaultPluginFileType = ".dll";

    /// <summary>
    /// Gets or sets the root directory to search for plugins.
    /// </summary>
    public string RootPluginsDirectory { get; set; }

    /// <summary>
    /// Gets or sets the underlying type of the plugin object.
    /// </summary>
    public Type PluginType { get; set; }

    /// <summary>
    /// Gets or sets the valid plugin file types to search for.
    /// Default: .dll
    /// </summary>
    public IEnumerable<string> ValidFileTypes { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public PluginFinderOptions()
    {
        RootPluginsDirectory = string.Empty;
        PluginType = typeof(Type);
        ValidFileTypes = new List<string> { DefaultPluginFileType };
    }
}