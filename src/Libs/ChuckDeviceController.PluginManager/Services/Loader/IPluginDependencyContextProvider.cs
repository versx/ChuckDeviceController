namespace ChuckDeviceController.PluginManager.Services.Loader
{
    using ChuckDeviceController.PluginManager.Services.Loader.Dependencies;

    public interface IPluginDependencyContextProvider
    {
        IPluginDependencyContext LoadFromPluginLoadContext(IPluginAssemblyLoadContext loadContext);
    }
}