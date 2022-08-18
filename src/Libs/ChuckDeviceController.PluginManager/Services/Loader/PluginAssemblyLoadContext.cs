namespace ChuckDeviceController.PluginManager.Services.Loader
{
    using System.Reflection;
    using System.Runtime.Loader;

    using Microsoft.Extensions.DependencyInjection;

    using ChuckDeviceController.PluginManager.Services.Loader.Runtime;
    using ChuckDeviceController.Plugins;

    public class PluginAssemblyLoadContext : AssemblyLoadContext
    {
        private readonly AssemblyDependencyResolver _resolver;

        #region Properties

        public string FullAssemblyPath { get; }

        public Type PluginType { get; }

        public IEnumerable<Type> HostTypes { get; set; }

        public IEnumerable<string> HostAssemblies { get; set; }

        public IEnumerable<Type> DowngradableHostTypes { get; set; }

        public IEnumerable<string> DowngradableHostAssemblies { get; set; }

        public IEnumerable<Type> RemoteTypes { get; set; }

        public PluginPlatformVersion PluginPlatformVersion { get; set; }

        public IRuntimePlatformContext RuntimePlatformContext { get; set; }

        public IEnumerable<string> AdditionalProbingPaths { get; set; }

        public string HostFramework { get; }

        public IServiceCollection HostServices { get; }

        public static PluginAssemblyLoadContext DefaultPluginLoadContext
        (
            string fullPathToPluginAssembly,
            Type pluginType,
            string hostFramework
        ) => new(fullPathToPluginAssembly, pluginType, hostFramework);

        #endregion

        #region Constructor

        public PluginAssemblyLoadContext(string pluginPath, Type pluginType, string hostFramework)
            : base(Path.GetFileNameWithoutExtension(pluginPath), isCollectible: true)
        {
            FullAssemblyPath = pluginPath;
            PluginType = pluginType;
            HostFramework = hostFramework;
            HostTypes = new[]
            {
                typeof(IPlugin),
                //typeof(IServiceCollection),
                typeof(ServiceCollection),
                //typeof(ControllerBase),
                //typeof(Controller),
            };
            HostAssemblies = new List<string>();
            DowngradableHostTypes = new[]
            {
                typeof(IPlugin),
            };
            DowngradableHostAssemblies = new List<string>();
            RemoteTypes = new[]
            {
                pluginType,
            };
            PluginPlatformVersion = PluginPlatformVersion.Create("1.0", RuntimeType.AspNetCoreAll);
            HostServices = new ServiceCollection();

            _resolver = new AssemblyDependencyResolver(pluginPath);
        }

        #endregion

        #region Overrides

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

        #endregion
    }
}