namespace ChuckDeviceController.PluginManager.Services.Finder
{
    using System.Reflection;

    using Microsoft.Extensions.Logging;

    using ChuckDeviceController.Logging;
    using ChuckDeviceController.PluginManager.Extensions;
    using ChuckDeviceController.PluginManager.Mvc.Extensions;
    using ChuckDeviceController.PluginManager.Services.Loader;

    /// <summary>
    /// Searches assemblies for plugin interface contract type.
    /// </summary>
    /// <typeparam name="TPlugin">Plugin interface contract type.</typeparam>
    public class PluginFinder<TPlugin> where TPlugin : class
    {
        private static readonly ILogger<PluginFinder<TPlugin>> _logger =
            GenericLoggerFactory.CreateLogger<PluginFinder<TPlugin>>();

        #region Properties

        public IPluginFinderOptions Options { get; }

        #endregion

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
            // Get list of list of files to separate each plugins references
            var assemblies = Options.RootPluginsDirectory.GetFiles(Options.ValidFileTypes);
            var pluginFinderResults = new List<PluginAssemblyDetails>();

            // Loop list of found assemblies and filter plugin assembly dependencies
            foreach (var assemblyPath in assemblies)
            {
                var pluginAssemblies = FindPluginInAssembly(assemblyPath);
                if (pluginAssemblies.Any())
                {
                    pluginFinderResults.AddRange(pluginAssemblies);
                }
            }
            return pluginFinderResults;
        }

        public IEnumerable<PluginAssemblyDetails> FindPluginInAssembly(string assemblyPath)
        {
            var pluginFinderResults = new List<PluginAssemblyDetails>();
            var assemblyFullPath = Path.GetFullPath(assemblyPath);

            try
            {
                // Get list of plugin types in assembly
                var pluginTypesFromAssembly = GetPluginTypesFromAssembly(assemblyFullPath);

                // Ensure assembly is a plugin assembly, otherwise skip
                if (!pluginTypesFromAssembly.Any())
                {
                    return pluginFinderResults;
                }

                foreach (var pluginImplementation in pluginTypesFromAssembly)
                {
                    var result = new PluginAssemblyDetails(assemblyFullPath, pluginImplementation);
                    pluginFinderResults.Add(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(exception: ex, $"Error occurred discovering plugins in assembly: '{assemblyFullPath}'");
            }

            return pluginFinderResults;
        }

        public IEnumerable<PluginFinderResult<TPlugin>> LoadPluginAssemblies(IEnumerable<PluginAssemblyDetails> pluginAssemblyDetails)
        {
            var entryAssembly = Assembly.GetEntryAssembly();
            var hostFramework = entryAssembly?.GetHostFramework();
            var results = new List<PluginFinderResult<TPlugin>>();
            foreach (var pluginAssembly in pluginAssemblyDetails)
            {
                var result = LoadPluginAssembly(pluginAssembly, hostFramework);
                if (result == null)
                {
                    _logger.LogWarning($"Failed to load plugin assembly '{pluginAssembly.AssemblyFullPath}'");
                    continue;
                }

                results.Add(result);
            }
            return results;
        }

        public PluginFinderResult<TPlugin> LoadPluginAssembly(PluginAssemblyDetails pluginAssemblyDetails, string hostFramework)
        {
            // Load plugin assembly in ALC
            var assemblyContext = PluginAssemblyLoadContext.Create<TPlugin>(pluginAssemblyDetails.AssemblyFullPath, hostFramework)
                // Add default Mvc and Mvc.Razor types
                .AddMvcRazorTypes();
            var assembly = assemblyContext.LoadFromAssemblyPath(pluginAssemblyDetails.AssemblyFullPath);

            var result = new PluginFinderResult<TPlugin>(
                assembly,
                assemblyContext,
                pluginAssemblyDetails.PluginTypeImplementation
            );
            return result;
        }

        #endregion

        #region Private Methods

        private static IEnumerable<Type> GetPluginTypesFromAssembly(string assemblyFullPath)
        {
            using var context = new PluginMetadataLoadContext(assemblyFullPath);
            var assemblyShim = context.LoadFromAssemblyPath(assemblyFullPath);
            var types = GetPluginTypesFromAssembly(assemblyShim);
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
            var types = assemblyTypes
                .Where(t => t.IsClass && !t.IsAbstract)
                .Where(t => t.GetInterfaces().Any(it => it.Name == pluginType.Name))
                .ToList();
            return types;
        }

        #endregion
    }
}