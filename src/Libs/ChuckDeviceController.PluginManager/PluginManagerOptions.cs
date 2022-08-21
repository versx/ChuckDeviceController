namespace ChuckDeviceController.PluginManager
{
    using Microsoft.Extensions.Configuration;

    public class PluginManagerOptions : IPluginManagerOptions
    {
        public string RootPluginsDirectory { get; set; }

        public IConfiguration Configuration { get; set; }

        public IReadOnlyDictionary<Type, object> SharedServiceHosts { get; set; }
        //public IServiceCollection SharedServiceHosts { get; set; }

        public PluginManagerOptions()
        {
        }

        public PluginManagerOptions(
            string rootPluginsDirectory,
            IConfiguration configuration,
            IReadOnlyDictionary<Type, object> sharedServiceHosts)
            //IServiceCollection sharedServiceHosts)
        {
            RootPluginsDirectory = rootPluginsDirectory;
            Configuration = configuration;
            SharedServiceHosts = sharedServiceHosts;
         }
    }
}