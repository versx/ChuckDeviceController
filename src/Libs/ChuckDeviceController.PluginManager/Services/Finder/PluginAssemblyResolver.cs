namespace ChuckDeviceController.PluginManager.Services.Finder
{
    using System.Reflection;
    using System.Runtime;
    using System.Runtime.Loader;

    using ChuckDeviceController.PluginManager.Extensions;

    public class PluginAssemblyResolver : MetadataAssemblyResolver
    {
        private readonly string _assemblyPath;
        private readonly string[] _platformAssemblies = new[]
        {
            "mscorlib",
            "netstandard",
            "System.Private.CoreLib",
            "System.Runtime",
        };

        protected IEnumerable<Assembly> GetLoadedAssemblies() => AssemblyLoadContext.Default.Assemblies;

        public PluginAssemblyResolver(string assemblyFullPath)
        {
            if (string.IsNullOrEmpty(assemblyFullPath))
            {
                throw new ArgumentNullException(nameof(assemblyFullPath));
            }
            _assemblyPath = assemblyFullPath.GetDirectoryName()!;
        }

        public override Assembly Resolve(MetadataLoadContext context, AssemblyName assemblyName)
        {
            // We know these assemblies are located in the Host, so don't bother loading them
            // from the plugin location
            if (_platformAssemblies.Contains(assemblyName.Name))
            {
                return LoadAssemblyForRuntime(context, assemblyName);
            }

            // Check if the file is found in the plugin location
            var candidateFile = Path.Combine(_assemblyPath, $"{assemblyName.Name}.dll");
            if (File.Exists(candidateFile))
            {
                return context.LoadFromAssemblyPath(candidateFile);
            }

            // Fallback, load from Host AppDomain, this is mostly required for System.* assemblies
            return LoadAssemblyForRuntime(context, assemblyName);
        }

        protected Assembly LoadAssemblyForRuntime(MetadataLoadContext context, AssemblyName assemblyName)
        {
            try
            {
                var loadedAssemblies = GetLoadedAssemblies();
                var candidate = loadedAssemblies.FirstOrDefault(assembly => assembly.GetName().Name == assemblyName.Name);
                var candidateName = candidate?.GetName();
                if (candidateName != null && candidateName.Version != assemblyName.Version)
                {
                    return context.LoadFromAssemblyPath(AssemblyLoadContext.Default.LoadFromAssemblyName(candidateName).Location);
                }
                return context.LoadFromAssemblyPath(AssemblyLoadContext.Default.LoadFromAssemblyName(assemblyName).Location);
            }
            catch (FileNotFoundException) when (assemblyName?.Name == "System.Runtime")
            {
                var hostRuntimeAssembly = typeof(GCSettings).GetTypeInfo().Assembly;
                throw new Exception($"System.Runtime {assemblyName.Version} failed to load. Are you trying to load a new plugin into an old host? Host Runtime Version: {hostRuntimeAssembly.GetName().Version} on {hostRuntimeAssembly.CodeBase}");
            }
        }
    }
}