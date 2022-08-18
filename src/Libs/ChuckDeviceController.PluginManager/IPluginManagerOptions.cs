namespace ChuckDeviceController.PluginManager
{
    using Microsoft.Extensions.Configuration;

    public interface IPluginManagerOptions
    {
        string RootPluginDirectory { get; }

        IConfiguration Configuration { get; }

        IReadOnlyDictionary<Type, object> SharedServiceHosts { get; }
    }
}