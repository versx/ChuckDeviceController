namespace ChuckDeviceController.PluginManager
{
    using Microsoft.Extensions.Configuration;

    public interface IPluginManagerOptions
    {
        string RootPluginsDirectory { get; }

        IConfiguration Configuration { get; }

        IReadOnlyDictionary<Type, object> SharedServiceHosts { get; }
    }
}