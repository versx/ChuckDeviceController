namespace ChuckDeviceConfigurator.Services.Plugins
{
    using System.Reflection;

    public class PluginFinderResult
    {
        public Type PluginType { get; }

        public string FullAssemblyPath { get; }

        public Assembly Assembly { get; }

        public PluginAssemblyLoadContext AssemblyLoadContext { get; }

        public PluginFinderResult(Type pluginType, string fullAssemblyPath, Assembly assembly, PluginAssemblyLoadContext assemblyLoadContext)
        {
            PluginType = pluginType;
            FullAssemblyPath = fullAssemblyPath;
            Assembly = assembly;
            AssemblyLoadContext = assemblyLoadContext;
        }
    }
}