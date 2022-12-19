namespace ChuckDeviceController.Plugin.Services;

/// <summary>
/// Assigns fields and properties in a plugin assembly with registered
/// service implementations.
/// </summary>
public interface IPluginBootstrapperServiceAttribute
{
    /// <summary>
    /// Gets or sets the bootstrap service contract type.
    /// </summary>
    Type ServiceType { get; }

    /// <summary>
    /// Gets or sets the bootstrap service implementation type.
    /// </summary>
    Type ProxyType { get; }
}