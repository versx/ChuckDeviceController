namespace ChuckDeviceController.PluginManager.Services.Finder
{
    using System.Reflection;

    using ChuckDeviceController.PluginManager.Extensions;
    using ChuckDeviceController.PluginManager.Mvc.Extensions;
    using ChuckDeviceController.PluginManager.Services.Loader;

    public class PluginFinder<TPlugin> where TPlugin : class
    {
        public IPluginFinderOptions Options { get; set; }

        #region Constructor

        public PluginFinder(IPluginFinderOptions? options = null)
        {
            Options = options ?? new PluginFinderOptions
            {
                PluginType = typeof(TPlugin),
                RootPluginsDirectory = PluginManager.DefaultPluginsFolder,
                ValidFileTypes = new List<string>
                {
                    PluginFinderOptions.DefaultPluginFileType,
                },
            };
        }

        #endregion

        #region Public Methods

        public IEnumerable<PluginAssemblyDetails> FindAssemliesWithPlugins()
        {
            // TODO: Get list of list of files to separate each plugins references
            var assemblies = Options.RootPluginsDirectory.GetFiles(Options.ValidFileTypes);
            var pluginFinderResults = new List<PluginAssemblyDetails>();

            // Loop list of found assemblies and filter plugin assembly dependencies
            foreach (var assemblyPath in assemblies)
            {
                try
                {
                    // Get list of plugin types in assembly
                    var assemblyFullPath = Path.GetFullPath(assemblyPath);
                    var pluginTypesFromAssembly = GetPluginTypesFromAssembly(assemblyFullPath);

                    // Ensure assembly is a plugin assembly, otherwise skip
                    if (!pluginTypesFromAssembly.Any())
                        continue;

                    foreach (var pluginImplementation in pluginTypesFromAssembly)
                    {
                        var result = new PluginAssemblyDetails(assemblyFullPath, pluginImplementation);
                        pluginFinderResults.Add(result);
                    }
                }
                catch //(Exception ex)
                {
                    continue;
                }
            }
            return pluginFinderResults;
        }

        public IEnumerable<PluginFinderResult<TPlugin>> LoadPluginAssemblies(IEnumerable<PluginAssemblyDetails> pluginAssemblyDetails)
        {
            var hostFramework = Assembly.GetEntryAssembly()?.GetHostFramework();
            var result = new List<PluginFinderResult<TPlugin>>();
            foreach (var pluginAssembly in pluginAssemblyDetails)
            {
                // Load plugin assembly in ALC
                var assemblyContext = PluginAssemblyLoadContext.Create<TPlugin>(pluginAssembly.AssemblyFullPath, hostFramework)
                    // Add default Mvc and Mvc.Razor types
                    .AddMvcRazorTypes();
                var assembly = assemblyContext.LoadFromAssemblyPath(pluginAssembly.AssemblyFullPath);

                // TODO: Add references to plugin ALC
                // TODO: Determine references/dependencies - managed as well as native

                result.Add(new PluginFinderResult<TPlugin>(
                    assembly,
                    assemblyContext,
                    pluginAssembly.PluginTypeImplementation
                ));
            }
            return result;
        }

        #endregion

        #region Private Methods

        private static IEnumerable<Type> GetPluginTypesFromAssembly(string assemblyFullPath)
        {
            IEnumerable<Type>? types;
            using (var context = new PluginMetadataLoadContext(assemblyFullPath))
            {
                var assemblyShim = context.LoadFromAssemblyPath(assemblyFullPath);
                types = GetPluginTypesFromAssembly(assemblyShim);
            }
            return types;
        }

        private static IEnumerable<Type> GetPluginTypesFromAssembly(IAssemblyShim assemblyShim)
        {
            var types = GetPluginTypes(assemblyShim.Types);
            return types;
        }

        private static IEnumerable<Type> GetPluginTypes(IEnumerable<Type> assemblyTypes)
        {
            var pluginType = typeof(TPlugin);
            var types = assemblyTypes.Where(t => t.IsClass && !t.IsAbstract)
                                     .Where(t => t.GetInterfaces().Any(it => it.Name == pluginType.Name))
                                     .ToList();
            return types;
        }

        #endregion
    }
}