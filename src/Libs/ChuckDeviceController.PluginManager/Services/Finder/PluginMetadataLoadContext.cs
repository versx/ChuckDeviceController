namespace ChuckDeviceController.PluginManager.Services.Finder
{
    using System.Reflection;

    public class PluginMetadataLoadContext : IPluginMetadataLoadContext
    {
        private readonly MetadataLoadContext _loadContext;

        public PluginMetadataLoadContext(string assemblyFullPath)
        {
            if (string.IsNullOrEmpty(assemblyFullPath))
            {
                throw new ArgumentNullException(nameof(assemblyFullPath));
            }
            _loadContext = new MetadataLoadContext(new PluginAssemblyResolver(assemblyFullPath));
        }

        public IAssemblyShim LoadFromAssemblyName(string assemblyName)
        {
            return new PluginAssembly(_loadContext.LoadFromAssemblyName(assemblyName));
        }

        public IAssemblyShim LoadFromAssemblyPath(string assemblyFullPath)
        {
            return new PluginAssembly(_loadContext.LoadFromAssemblyPath(assemblyFullPath));
        }

        public void Dispose()
        {
            _loadContext?.Dispose();
        }
    }
}