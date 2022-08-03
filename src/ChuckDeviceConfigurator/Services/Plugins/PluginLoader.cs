namespace ChuckDeviceConfigurator.Services.Plugins
{
    using System.Reflection;

    using ChuckDeviceController.Plugins;

    public class PluginLoader
    {
        public IEnumerable<IPlugin> LoadedPlugins { get; }

        public PluginLoader(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new Exception($"Plugin does not exist at '{filePath}'");
            }

            var assembly = LoadPlugin(filePath);
            if (assembly == null)
            {
                throw new NullReferenceException($"Failed to load plugin assembly: '{filePath}'");
            }
            LoadedPlugins = CreatePlugins(assembly);
        }

        private static Assembly LoadPlugin(string relativePath)
        {
            var loadContext = new PluginLoadContext(relativePath);
            var fileName = Path.GetFileNameWithoutExtension(relativePath);
            return loadContext.LoadFromAssemblyName(new AssemblyName(fileName));
        }

        private static IEnumerable<IPlugin> CreatePlugins(Assembly assembly)
        {
            var count = 0;
            var args = new object[] { new AppHost(), new LoggingHost() };
            foreach (var type in assembly.GetTypes())
            {
                if (typeof(IPlugin).IsAssignableFrom(type))
                {
                    if (Activator.CreateInstance(type, args) is IPlugin result)
                    {
                        count++;
                        yield return result;
                    }
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
    }
}

