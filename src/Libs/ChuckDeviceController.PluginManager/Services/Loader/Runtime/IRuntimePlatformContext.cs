namespace ChuckDeviceController.PluginManager.Services.Loader.Runtime;

public interface IRuntimePlatformContext
{
    IEnumerable<string> GetPlatformExtensions();

    IEnumerable<string> GetPluginDependencyNames(string fileName);

    IEnumerable<string> GetPlatformDependencyNames(string fileName);

    RuntimeInfo GetRuntimeInfo();
}