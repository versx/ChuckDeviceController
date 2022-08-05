namespace ChuckDeviceConfigurator.Services.Plugins
{
    using System.Reflection;

    using ChuckDeviceController.Plugins;

    public class PluginLoader<TPlugin> : IPluginLoader<TPlugin> where TPlugin : class, IPlugin
    {
        #region Variables

        private static readonly ILogger<IPluginLoader<TPlugin>> _logger =
            new Logger<IPluginLoader<TPlugin>>(LoggerFactory.Create(x => x.AddConsole()));

        #endregion

        #region Properties

        public string PluginFilePath { get; }

        public IEnumerable<IPlugin> LoadedPlugins { get; }

        #endregion

        #region Constructor

        public PluginLoader(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new Exception($"Plugin does not exist at '{filePath}'");
            }

            PluginFilePath = filePath;

            var assembly = LoadAssembly(PluginFilePath);
            if (assembly == null)
            {
                throw new NullReferenceException($"Failed to load plugin assembly: '{PluginFilePath}'");
            }

            LoadedPlugins = CreatePlugins(assembly);
        }

        #endregion

        #region Public Methods

        public Assembly? LoadDefaultAssembly()
        {
            return LoadAssemblyFromPath(PluginFilePath);
        }

        /// <summary>
        /// Load an assembly from path.
        /// </summary>
        /// <param name="assemblyPath">The assembly path.</param>
        /// <returns>The assembly.</returns>
        public Assembly? LoadAssemblyFromPath(string assemblyPath)
            => LoadAssembly(assemblyPath);

        #endregion

        #region Private Methods

        private static Assembly? LoadAssembly(string pluginPath)
        {
            var loadContext = new PluginLoadContext(pluginPath);
            var fileName = Path.GetFileNameWithoutExtension(pluginPath);
            return loadContext.LoadFromAssemblyName(new AssemblyName(fileName));
        }

        private static IEnumerable<IPlugin> CreatePlugins(Assembly assembly)
        {
            var count = 0;
            var pluginTypes = PluginFinder<IPlugin>.GetPluginTypes(assembly);
            foreach (var pluginType in pluginTypes)
            {
                var parameters = BuildConstructorParameters(pluginType);
                if (Activator.CreateInstance(pluginType, parameters) is IPlugin result)
                {
                    count++;
                    yield return result;
                }
            }

            if (count == 0)
            {
                var availableTypes = string.Join(",", assembly.GetTypes().Select(t => t.FullName));
                throw new ApplicationException(
                    $"Can't find any type which implements {nameof(IPlugin)} in {assembly} from {assembly.Location}.\n" +
                    $"Available types: {availableTypes}");
            }
        }

        private static object[] BuildConstructorParameters(Type pluginType)
        {
            var list = new List<object>();
            var constructorInfo = pluginType.GetConstructors()[0];
            var parameters = constructorInfo.GetParameters();

            foreach (var param in parameters)
            {
                if (typeof(ILoggingHost) == param.ParameterType)
                    list.Add(new LoggingHost());
                else if (typeof(IUiHost) == param.ParameterType)
                    list.Add(new UiHost());
                else if (typeof(IDatabaseHost) == param.ParameterType)
                    list.Add(new DatabaseHost());
                // TODO: else if (typeof(IJobControllerServiceHost) == param.ParameterType)
                //    list.Add(_jobControllerService);
            }

            var instance = Activator.CreateInstance(pluginType, list.ToArray());
            if (instance == null)
            {
                _logger.LogError($"Failed to initialize new instance of Plugin '{pluginType.Name}'");
                return null;
            }

            // TOOD: PluginHost to contain event handlers class(es)
            //var objectValue = GetObjectValue(instance);
            //foreach (var type in pluginType.GetInterfaces())
            //{
            //    if (typeof(IAppEvents) == type)
            //        _pluginHandlers.AppEvents = (IAppEvents)objectValue;
            //    else if (typeof(IUiEvents) == type)
            //        _pluginHandlers.UiEvents = (IUiEvents)objectValue;
            //}
            return list.ToArray();
        }

        private static object GetObjectValue(object o)
        {
            return System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(o);
        }

        #endregion
    }
}