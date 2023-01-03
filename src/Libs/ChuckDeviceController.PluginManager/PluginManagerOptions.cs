namespace ChuckDeviceController.PluginManager;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public class PluginManagerOptions : IPluginManagerOptions
{
    public string RootPluginsDirectory { get; set; } = null!;

    public IConfiguration Configuration { get; set; } = null!;

    public IReadOnlyDictionary<Type, object> SharedServiceHosts { get; set; } = null!;

    public IServiceCollection Services { get; set; } = null!;

    public ServiceProvider ServiceProvider { get; set; } = null!;

    public PluginManagerOptions()
    {
    }

    public PluginManagerOptions(
        string rootPluginsDirectory,
        IConfiguration configuration,
        IServiceCollection services,
        ServiceProvider serviceProvider,
        IReadOnlyDictionary<Type, object> sharedServiceHosts)
    {
        RootPluginsDirectory = rootPluginsDirectory;
        Configuration = configuration;
        Services = services;
        ServiceProvider = serviceProvider;
        SharedServiceHosts = sharedServiceHosts;
     }
}