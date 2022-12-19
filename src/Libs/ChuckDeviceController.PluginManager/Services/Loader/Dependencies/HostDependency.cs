namespace ChuckDeviceController.PluginManager.Services.Loader.Dependencies;

using System.Reflection;

public class HostDependency
{
    public AssemblyName DependencyName { get; set; } = null!;

    public Version? Version => DependencyName?.Version;

    public bool AllowDowngrade { get; set; }
}