namespace ChuckDeviceController.PluginManager.Services.Finder
{
    public interface IPluginMetadataLoadContext : IDisposable
    {
        IAssemblyShim LoadFromAssemblyName(string assemblyName);

        IAssemblyShim LoadFromAssemblyPath(string assemblyFullPath);
    }
}
