namespace ChuckDeviceController.PluginManager.Services.Loader
{
    using System.Reflection;
    using System.Runtime.Loader;

    using Microsoft.Extensions.DependencyInjection;

    using ChuckDeviceController.Plugin;
    using ChuckDeviceController.PluginManager.Services.Loader.Dependencies;
    using ChuckDeviceController.PluginManager.Services.Loader.Runtime;

    public class PluginAssemblyLoadContext : AssemblyLoadContext, IPluginAssemblyLoadContext
    {
        #region Variables

        private readonly AssemblyDependencyResolver _resolver;
        private readonly IPluginDependencyContextProvider _pluginDependencyContextProvider;

        #endregion

        #region Properties

        public string AssemblyFullPath { get; }

        public Type PluginType { get; }

        public IEnumerable<Type> HostTypes { get; set; }

        public IEnumerable<string> HostAssemblies { get; set; }

        public IServiceCollection HostServices { get; }

        public string HostFramework { get; }

        public IEnumerable<Type> DowngradableHostTypes { get; set; }

        public IEnumerable<string> DowngradableHostAssemblies { get; set; }

        public IEnumerable<Type> RemoteTypes { get; set; }

        public PluginPlatformVersion PluginPlatformVersion { get; set; }

        public IEnumerable<string> AdditionalProbingPaths { get; set; }

        public bool IgnorePlatformInconsistencies { get; set; }

        public IPluginDependencyContext DependencyContext { get; }

        public static PluginAssemblyLoadContext DefaultPluginLoadContext
        (
            string assemblyFullPath,
            Type pluginType,
            string hostFramework
        ) => new(assemblyFullPath, pluginType, hostFramework);

        #endregion

        #region Constructor

        public PluginAssemblyLoadContext(string pluginPath, Type pluginType, string hostFramework, bool ignorePlatformInconsistencies = false)
            : base(Path.GetFileNameWithoutExtension(pluginPath), isCollectible: true)
        {
            AssemblyFullPath = pluginPath;
            PluginType = pluginType;
            HostTypes = new[]
            {
                typeof(IPlugin),
                //typeof(IServiceCollection),
                typeof(ServiceCollection),
                //typeof(ControllerBase),
                //typeof(Controller),
            };
            HostAssemblies = new List<string>();
            HostServices = new ServiceCollection();
            HostFramework = hostFramework;
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
            AdditionalProbingPaths = new List<string>();
            IgnorePlatformInconsistencies = ignorePlatformInconsistencies;

            _resolver = new AssemblyDependencyResolver(pluginPath);
            _pluginDependencyContextProvider = new PluginDependencyContextProvider();
            DependencyContext = _pluginDependencyContextProvider.LoadFromPluginLoadContext(this);
        }

        #endregion

        #region Public Static Methods

        public static PluginAssemblyLoadContext Create<TPlugin>(string pluginPath, string hostFramework)
        {
            var loadContext = new PluginAssemblyLoadContext(
                pluginPath,
                typeof(TPlugin),
                hostFramework
            );
            return loadContext;
        }

        #endregion

        #region Protected Overrides

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
            if (string.IsNullOrEmpty(assemblyPath))
                return null;

            return LoadFromAssemblyPath(assemblyPath);
        }

        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            var libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
            if (string.IsNullOrEmpty(libraryPath))
                return IntPtr.Zero;

            return LoadUnmanagedDllFromPath(libraryPath);
        }

        #endregion
    }
}