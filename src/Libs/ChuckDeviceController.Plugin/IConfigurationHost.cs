namespace ChuckDeviceController.Plugin;

using Microsoft.Extensions.Configuration;

/// <summary>
///     This interface contract can be used by all plugin modules to load setting and configuration data.
/// 
///     The default implementation which is loaded if no other plugin registers an instance uses 
///     appsettings.json to store configuration data to be used by Plugins.
/// 
///     An instance of this interface is available via the DI container, any custom implementations
///     must be configured to be used in the DI contaner when being initialized.
/// </summary>
/// <remarks>
///     This class can be customised by the host application, if no implementation is provided then
///     a default implementation is provided.
/// </remarks>
public interface IConfigurationHost
{
    /// <summary>
    ///     Retrieves a configuration instance.
    /// </summary>
    /// <param name="jsonFileName">
    ///     Name of the JSON file name to be used. If a JSON cofiguration file is not provided, the default
    ///     'appsettings.json' will be loaded from the calling plugin's root folder.
    /// </param>
    /// <param name="sectionName">
    ///     The name of the configuration section that might be required.
    /// </param>
    /// <returns>
    ///     Configuration file instance initialized with the required settings.
    /// </returns>
    IConfiguration GetConfiguration(string? jsonFileName = null, string? sectionName = null);

    /// <summary>
    ///     Retrieves a value from a JSON configuration file.
    /// </summary>
    /// <typeparam name="T">The class related to the settings being requested.</typeparam>
    /// <param name="name">Name of the property to retrieve the value for.</param>
    /// <param name="defaultValue">Default value to return.</param>
    /// <param name="sectionName">The name of the configuration section that might be required.</param>
    /// <returns>Returns the value related to the named configuration property.</returns>
    T? GetValue<T>(string name, T? defaultValue = default, string? sectionName = null);
}