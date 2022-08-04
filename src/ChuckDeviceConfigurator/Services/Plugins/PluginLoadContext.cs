namespace ChuckDeviceConfigurator.Services.Plugins
{
    using System;
    using System.Reflection;
    using System.Runtime.Loader;

    public class PluginLoadContext : AssemblyLoadContext
    {
        private readonly AssemblyDependencyResolver _resolver;

        public PluginLoadContext(string pluginPath)
            : base(Path.GetFileNameWithoutExtension(pluginPath), isCollectible: true)
        {
            _resolver = new AssemblyDependencyResolver(pluginPath);
        }

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
            if (assemblyPath != null)
            {
                return LoadFromAssemblyPath(assemblyPath);
            }
            return null;
        }

        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            var libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
            if (libraryPath != null)
            {
                return LoadUnmanagedDllFromPath(libraryPath);
            }
            return IntPtr.Zero;
        }
    }

    public class PluginAssemblyLoadContext : AssemblyLoadContext
    {
        public PluginAssemblyLoadContext(string name)
            : base(name, isCollectible: true)
        {
        }
    }
}