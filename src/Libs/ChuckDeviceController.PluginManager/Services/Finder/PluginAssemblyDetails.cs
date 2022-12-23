namespace ChuckDeviceController.PluginManager.Services.Finder;

public class PluginAssemblyDetails
{
    public string AssemblyFullPath { get; }

    public Type PluginTypeImplementation { get; }

    public DateTime LoadedAt { get; set; }

    public PluginAssemblyDetails(string assemblyFullPath, Type pluginImplementation, DateTime? loadedAt = null)
    {
        AssemblyFullPath = assemblyFullPath;
        PluginTypeImplementation = pluginImplementation;
        LoadedAt = loadedAt ?? DateTime.Now;
    }
}