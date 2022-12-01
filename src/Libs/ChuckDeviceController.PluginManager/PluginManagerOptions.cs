namespace ChuckDeviceController.PluginManager
{
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;

    public class PluginManagerOptions : IPluginManagerOptions
    {
        public string RootPluginsDirectory { get; set; } = null!;

        public IConfiguration? Configuration { get; set; }

        public IReadOnlyDictionary<Type, object> SharedServiceHosts { get; set; } = null!;

        public IServiceCollection? Services { get; set; }

        public PluginManagerOptions()
        {
        }

        public PluginManagerOptions(
            string rootPluginsDirectory,
            IConfiguration configuration,
            IServiceCollection services,
            IReadOnlyDictionary<Type, object> sharedServiceHosts)
        {
            RootPluginsDirectory = rootPluginsDirectory;
            Configuration = configuration;
            Services = services;
            SharedServiceHosts = sharedServiceHosts;
         }
    }
}