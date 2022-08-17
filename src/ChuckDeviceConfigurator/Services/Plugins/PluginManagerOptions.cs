namespace ChuckDeviceConfigurator.Services.Plugins
{
    public class PluginManagerOptions : IPluginManagerOptions
    {
        public string RootPluginDirectory { get; set; }

        public IConfiguration Configuration { get; set; }

        public IReadOnlyDictionary<Type, object> SharedServiceHosts { get; set; }
        //public IServiceCollection SharedServiceHosts { get; set; }

        public PluginManagerOptions()
        {
        }

        public PluginManagerOptions(string rootPluginsDirectory, IConfiguration configuration, IReadOnlyDictionary<Type, object> sharedServiceHosts)
        //public PluginManagerOptions(string rootPluginsDirectory, IConfiguration configuration, IServiceCollection sharedServiceHosts)
        {
            RootPluginDirectory = rootPluginsDirectory;
            Configuration = configuration;
            SharedServiceHosts = sharedServiceHosts;
         }
    }
}