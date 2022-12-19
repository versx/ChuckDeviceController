namespace ChuckDeviceController.PluginManager.Services.Loader.Runtime;

/// <summary>
/// Specifies which runtime type the plugin targets.
/// </summary>
public enum RuntimeType
{
    /// <summary>
    /// No runtime found or specified.
    /// </summary>
    None = 0,

    /// <summary>
    /// ASP.NET Core 2.x targeting .NET Core 2.x and earlier.
    /// </summary>
    AspNetCoreAll,

    /// <summary>
    /// ASP.NET Core Web application targetting .NET Core 3.0 and later.
    /// </summary>
    AspNetCoreApp,

    /// <summary>
    /// Targets standard .NET Core runtime.
    /// </summary>
    NetCoreApp,

    /// <summary>
    /// Targets .NET Desktop Runtime.
    /// </summary>
    WindowsDesktopApp,
}