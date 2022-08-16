namespace ChuckDeviceConfigurator.Services.Plugins
{
    public interface IPluginManagerOptions
    {
        string? RootPluginDirectory { get; }

        IConfiguration Configuration { get; }

        IReadOnlyDictionary<Type, object> SharedServiceHosts { get; }
    }
}