namespace ChuckDeviceConfigurator.Services.Plugins
{
    using System.Reflection;

    using ChuckDeviceController.Plugins;

    public class PluginLoader<TPlugin> : IPluginLoader<TPlugin> where TPlugin : class, IPlugin
    {
        #region Variables

        private static readonly ILogger<IPluginLoader<TPlugin>> _logger =
            new Logger<IPluginLoader<TPlugin>>(LoggerFactory.Create(x => x.AddConsole()));
        private readonly Dictionary<Type, object> _sharedHosts = new();

        #endregion

        #region Properties

        public string PluginFilePath { get; }

        public IEnumerable<IPlugin> LoadedPlugins { get; }

        #endregion

        #region Constructor

        public PluginLoader(string filePath, Dictionary<Type, object> sharedHosts)
        {
            if (!File.Exists(filePath))
            {
                throw new Exception($"Plugin does not exist at '{filePath}'");
            }

            PluginFilePath = filePath;
            _sharedHosts = sharedHosts;

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
        /// <returns>The loaded assembly.</returns>
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

        private IEnumerable<IPlugin> CreatePlugins(Assembly assembly)
        {
            var count = 0;
            var pluginTypes = PluginFinder<IPlugin>.GetPluginTypes(assembly);
            foreach (var pluginType in pluginTypes)
            {
                var plugin = LoadPluginWithDataParameters(pluginType);
                if (plugin != null)
                {
                    count++;
                    yield return plugin;
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

        private IPlugin LoadPluginWithDataParameters(Type pluginType)
        {
            // Check that there is only one constructor configured
            var constructors = pluginType.GetConstructors();
            if ((constructors?.Length ?? 0) == 0)
            {
                _logger.LogError($"Plugins must only contain one constructor for each class that inherits from '{nameof(IPlugin)}', skipping registration for plugin '{pluginType.Name}'");
                return null;
            }

            var constructorInfo = constructors![0];
            var parameters = constructorInfo.GetParameters();
            var list = new List<object>(parameters.Length);

            // Check that we were provided shared host types
            if ((_sharedHosts?.Count ?? 0) > 0)
            {
                // Loop the plugin's constructor parameters to see which host type handlers
                // to provide it when we instantiate a new instance.
                foreach (var param in parameters)
                {
                    if (!_sharedHosts!.ContainsKey(param.ParameterType))
                        continue;

                    var pluginHostHandler = _sharedHosts[param.ParameterType];
                    list.Add(pluginHostHandler);
                }
            }

            IPlugin? instance;
            try
            {
                var args = list.ToArray();
                instance = (IPlugin?)Activator.CreateInstance(pluginType, args);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to instantiate a new instance of Plugin '{pluginType.Name}': {ex}");
                return null;
            }

            if (instance == null)
            {
                _logger.LogError($"Failed to instantiate a new instance of Plugin '{pluginType.Name}'");
                return null;
            }

            // TOOD: PluginHost to contain host event handler class(es)
            var objectValue = GetObjectValue(instance);
            //foreach (var type in pluginType.GetInterfaces())
            //{
            //    if (typeof(IUiEvents) == type)
            //        _pluginHandlers.UiEvents = (IUiEvents)objectValue;
            //    else if (typeof(IDatabaseEvents) == type)
            //        _pluginHandlers.DatabaseEvents = (IDatabaseEvents)objectValue;
            //    else if (typeof(IJobControllerServiceHost) == type)
            //        _pluginHandlers.JobControllerEvents = (IJobControllerServiceHost)objectValue;
            //}
            //return list.ToArray();
            return (IPlugin)objectValue;
        }

        private static object GetObjectValue(object o)
        {
            return System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(o);
        }

        #endregion
    }
}