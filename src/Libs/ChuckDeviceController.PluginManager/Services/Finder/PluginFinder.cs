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

        public IReadOnlyList<PluginFinderResult<TPlugin>> FindAssemliesWithPlugins()
        {
            // TODO: Get list of list of files to separate each plugins references
            var assemblies = Options.RootPluginsDirectory.GetFiles(Options.ValidFileTypes);
            var hostFramework = Assembly.GetEntryAssembly()?.GetHostFramework();
            var pluginFinderResults = new List<PluginFinderResult<TPlugin>>();
            foreach (var assemblyPath in assemblies)
            {
                try
                {
                    var assemblyContext = PluginAssemblyLoadContext.Load<TPlugin>(assemblyPath, hostFramework);
                    var assemblyFullPath = Path.GetFullPath(assemblyPath);
                    var assembly = assemblyContext.LoadFromAssemblyPath(assemblyFullPath);
                    if (!GetPluginTypes(assembly).Any())
                        continue;

                    // TODO: Add references to plugin ALC
                    // TODO: Determine references/dependencies - managed as well as native

                    // Add default Mvc and Mvc.Razor types
                    assemblyContext.AddMvcRazorTypes();

                    var loaderContext = CreateResult(assembly, assemblyContext);
                    pluginFinderResults.Add(loaderContext);
                }
                catch //(Exception ex)
                {
                    continue;
                }
            }
            return pluginFinderResults;
        }

        public static IReadOnlyList<Type> GetPluginTypes(Assembly assembly)
        {
            try
            {
                var types = assembly.GetTypes()
                                    .Where(type => typeof(TPlugin).IsAssignableFrom(type))
                                    .Where(type => type.IsClass && !type.IsAbstract)
                                    .ToList();
                return types;
            }
            catch
            {
                return default;
            }
        }

        #endregion

        private static PluginFinderResult<TPlugin> CreateResult(Assembly assembly, PluginAssemblyLoadContext loadContext)
        {
            var loaderContext = new PluginFinderResult<TPlugin>(
                assembly,
                loadContext
            );
            return loaderContext;
        }
    }
}