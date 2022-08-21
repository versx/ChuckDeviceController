namespace ChuckDeviceController.PluginManager.Services.Finder
{
    using System.Reflection;

    using ChuckDeviceController.PluginManager.Services.Loader;

    public class PluginFinderResult<TPlugin>
    {
        public Type PluginType { get; }

        public Type PluginTypeImplementation { get; }

        public Assembly Assembly { get; }

        public string? AssemblyPath => Assembly?.Location;

        public PluginAssemblyLoadContext AssemblyLoadContext { get; }

        public PluginFinderResult(
            Assembly assembly,
            PluginAssemblyLoadContext assemblyLoadContext,
            Type pluginImplementation)
        {
            Assembly = assembly;
            AssemblyLoadContext = assemblyLoadContext;
            PluginType = typeof(TPlugin);
            PluginTypeImplementation = pluginImplementation;
        }
    }
}