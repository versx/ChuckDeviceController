namespace ChuckDeviceController.PluginManager.Services.Loader.Dependencies;

using System.Reflection;

public class RemoteDependency
{
    public AssemblyName DependencyName { get; set; } = null!;

    public Version? Version => DependencyName?.Version;
}