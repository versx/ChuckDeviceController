namespace ChuckDeviceController.PluginManager.Services.Finder
{
    public class PluginAssemblyDetails
    {
        public string AssemblyFullPath { get; }

        public Type PluginTypeImplementation { get; }

        public PluginAssemblyDetails(string assemblyFullPath, Type pluginImplementation)
        {
            AssemblyFullPath = assemblyFullPath;
            PluginTypeImplementation = pluginImplementation;
        }
    }
}