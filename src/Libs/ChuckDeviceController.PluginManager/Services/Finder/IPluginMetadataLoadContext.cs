namespace ChuckDeviceController.PluginManager.Services.Finder;

using System.Reflection;

public interface IPluginMetadataLoadContext : IDisposable
{
    IAssemblyShim LoadFromAssemblyName(AssemblyName assemblyName);

    IAssemblyShim LoadFromAssemblyPath(string assemblyFullPath);
}
