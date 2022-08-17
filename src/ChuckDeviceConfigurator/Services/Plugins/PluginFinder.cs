namespace ChuckDeviceConfigurator.Services.Plugins
{
    using System.Reflection;

    using ChuckDeviceConfigurator.Services.Plugins.Extensions;
    using ChuckDeviceConfigurator.Services.Plugins.Mvc.Extensions;

    public class PluginFinder<TPlugin> where TPlugin : class//, IPlugin
    {
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
            var hostFramework = Assembly.GetEntryAssembly()?.GetHostFramework();
            var pluginFinderResults = new List<PluginFinderResult>();
            foreach (var assemblyPath in assemblies)
            {
                try
                {
                    var assemblyContext = new PluginAssemblyLoadContext(assemblyPath, typeof(TPlugin), hostFramework);
                    var assemblyFullPath = Path.GetFullPath(assemblyPath);
                    var assembly = assemblyContext.LoadFromAssemblyPath(assemblyFullPath);
                    if (!GetPluginTypes(assembly).Any())
                        continue;

                    //assemblyContext.AddMvcTypes();
                    //assemblyContext.AddMvcRazorTypes();

                    var loaderContext = new PluginFinderResult(
                        typeof(TPlugin),
                        assembly.Location,
                        assembly,
                        assemblyContext
                    );
                    pluginFinderResults.Add(loaderContext);
                }
                catch //(Exception ex)
                {
                    continue;
                }
            }
            //pluginFinderAssemblyContext.Unload();
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
    }
}