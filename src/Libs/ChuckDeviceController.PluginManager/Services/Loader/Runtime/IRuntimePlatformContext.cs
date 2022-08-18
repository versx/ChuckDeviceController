namespace ChuckDeviceController.PluginManager.Services.Loader.Runtime
{
    public interface IRuntimePlatformContext
    {
        IEnumerable<string> GetPlatformExtensions();

        IEnumerable<string> GetPluginDependencyNames(string nameWithoutExtension);

        IEnumerable<string> GetPlatformDependencyNames(string nameWithoutExtension);

        RuntimeInfo GetRuntimeInfo();
    }
}