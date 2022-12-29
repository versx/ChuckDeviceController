namespace ChuckDeviceController.Plugin;

/// <summary>
/// Defines the API key used by a plugin.
/// </summary>
[AttributeUsage(validOn: AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class PluginApiKeyAttribute : Attribute
{
    /// <summary>
    /// Gets the API key string used by the plugin.
    /// </summary>
    public string ApiKey { get; }

    /// <summary>
    /// Instantiates a new instance of the <see cref="PluginApiKeyAttribute"/>
    /// attribute class.
    /// </summary>
    /// <param name="apiKey"></param>
    public PluginApiKeyAttribute(string apiKey)
    {
        ApiKey = apiKey;
    }
}