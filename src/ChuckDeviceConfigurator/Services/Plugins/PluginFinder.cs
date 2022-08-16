namespace ChuckDeviceConfigurator.Services.Plugins
{
    using System.Reflection;

    using ChuckDeviceConfigurator.Services.Plugins.Extensions;

    public class PluginFinder<TPlugin> where TPlugin : class//, IPlugin
    {
        private const string AssemblyContextName = "PluginFinderAssemblyContext";

        public IPluginFinderOptions Options { get; set; }

        #region Constructor

        public PluginFinder(IPluginFinderOptions? options = null)
        {
            Options = options ?? new PluginFinderOptions
            {
                PluginType = typeof(TPlugin),
                RootPluginsDirectory = Strings.PluginsFolder,
                ValidFileTypes = new List<string>
                {
                    PluginFinderOptions.DefaultPluginFileType,
                },
            };
        }

        #endregion

        #region Public Methods

        public IReadOnlyList<PluginFinderResult> FindAssemliesWithPlugins()
        {
            var assemblies = Options.RootPluginsDirectory.GetFiles(Options.ValidFileTypes);
            return FindPluginsInAssemblies(assemblies);
        }

        public static IReadOnlyList<Type> GetPluginTypes(Assembly assembly)
        {
            var types = assembly.GetTypes()
                                .Where(type => typeof(TPlugin).IsAssignableFrom(type))
                                .Where(type => type.IsClass && !type.IsAbstract)
                                .ToList();
            return types;
        }

        #endregion

        #region Private Methods

        private static IReadOnlyList<PluginFinderResult> FindPluginsInAssemblies(IEnumerable<string> assemblyPaths)
        {
            var pluginFinderResults = new List<PluginFinderResult>();
            var pluginFinderAssemblyContext = new PluginAssemblyLoadContext(AssemblyContextName);
            foreach (var assemblyPath in assemblyPaths)
            {
                var assemblyFullPath = Path.GetFullPath(assemblyPath);
                var assembly = pluginFinderAssemblyContext.LoadFromAssemblyPath(assemblyFullPath);
                if (!GetPluginTypes(assembly).Any())
                    continue;

                var loaderContext = new PluginFinderResult(
                    typeof(TPlugin),
                    assembly.Location,
                    assembly,
                    pluginFinderAssemblyContext
                );
                pluginFinderResults.Add(loaderContext);
            }
            //pluginFinderAssemblyContext.Unload();
            return pluginFinderResults;
        }

        #endregion
    }
}