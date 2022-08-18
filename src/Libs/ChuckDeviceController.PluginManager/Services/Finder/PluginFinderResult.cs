namespace ChuckDeviceController.PluginManager.Services.Finder
{
    using System.Reflection;

    using ChuckDeviceController.PluginManager.Services.Loader;

    public class PluginFinderResult<TPlugin>
    {
        public Type PluginType { get; }

        public string FullAssemblyPath { get; }

        public Assembly Assembly { get; }

        public PluginAssemblyLoadContext AssemblyLoadContext { get; }

        public PluginFinderResult(
            Assembly assembly,
            PluginAssemblyLoadContext assemblyLoadContext)
        {
            PluginType = typeof(TPlugin);
            FullAssemblyPath = assembly.Location;
            Assembly = assembly;
            AssemblyLoadContext = assemblyLoadContext;
        }
    }
}