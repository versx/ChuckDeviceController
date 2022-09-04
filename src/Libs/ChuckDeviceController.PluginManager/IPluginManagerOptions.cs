namespace ChuckDeviceController.PluginManager
{
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;

    public interface IPluginManagerOptions
    {
        string RootPluginsDirectory { get; }

        IConfiguration Configuration { get; }

        IServiceCollection Services { get; }

        IReadOnlyDictionary<Type, object> SharedServiceHosts { get; }
    }
}