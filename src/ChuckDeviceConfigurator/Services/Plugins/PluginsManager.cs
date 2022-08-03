namespace ChuckDeviceConfigurator.Services.Plugins
{
    using System;
    using System.Reflection;

    using ChuckDeviceController.Plugins;

    public class PluginsManager : IPluginsManager
    {
        #region Variables

        private readonly ILogger<IPluginsManager> _logger;
        private readonly Dictionary<string, IPlugin> _plugins = new();

        #endregion

        public IReadOnlyDictionary<string, IPlugin> Plugins => _plugins;
        // TODO: Singleton pattern

        public PluginsManager(ILogger<IPluginsManager> logger)
        {
            _logger = logger;
        }

        public async Task LoadPluginsAsync(string pluginsFolder)
        {
            if (!Directory.Exists(pluginsFolder))
            {
                throw new DirectoryNotFoundException($"Plugins folder '{pluginsFolder}' does not exist!");
            }

            var files = Directory.GetFiles(pluginsFolder);
            foreach (var file in files)
            {
                var loader = new PluginLoader(file);
                if (!loader.LoadedPlugins.Any())
                {
                    _logger.LogWarning($"No plugins loaded from plugin '{file}'");
                    continue;
                }

                foreach (var plugin in loader.LoadedPlugins)
                {
                    await plugin.InitializeAsync();
                    if (_plugins.ContainsKey(plugin.Name))
                    {
                        _logger.LogWarning($"Plugin with name '{plugin.Name}' already loaded, skipping...");
                        continue;
                    }
                    _plugins.Add(plugin.Name, plugin);
                    _logger.LogInformation($"Plugin '{plugin.Name}' v{plugin.Version} by {plugin.Author} added to plugin manager cache.");
                }
            }
        }
    }

    public class AppHost : IAppHost
    {
        public void Restart()
        {
            Console.WriteLine($"Restart");
        }

        public void Shutdown()
        {
            Console.WriteLine($"Shutdown");
        }

        public void Uninstall()
        {
            Console.WriteLine($"Uninstall");
        }
    }

    public class LoggingHost : ILoggingHost
    {
        public void LogException(Exception ex)
        {
            Console.WriteLine($"Exception occurred from plugin: {ex}");
        }

        public void LogMessage(string text, params object[] args)
        {
            Console.WriteLine($"Message from plugin: {text}", args);
        }
    }
}