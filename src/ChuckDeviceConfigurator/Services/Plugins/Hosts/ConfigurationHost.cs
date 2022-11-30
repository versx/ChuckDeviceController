namespace ChuckDeviceConfigurator.Services.Plugins.Hosts
{
    using System.Reflection;

    using Microsoft.Extensions.Configuration;

    using ChuckDeviceController.Plugin;
    using ChuckDeviceController.PluginManager.Extensions;

    public class ConfigurationHost : IConfigurationHost
    {
        private const string DefaultConfigurationFileName = "appsettings.json";

        // Cache loaded configuration files
        private readonly Dictionary<string, IConfiguration> _configurations = new();

        public string RootPath { get; }

        #region Constructors

        public ConfigurationHost()
            : this(Strings.PluginsFolder)
        {
        }

        public ConfigurationHost(string rootPath)
        {
            if (string.IsNullOrEmpty(rootPath))
            {
                throw new ArgumentNullException(nameof(rootPath));
            }

            if (!Directory.Exists(rootPath))
            {
                throw new DirectoryNotFoundException();
            }

            RootPath = Path.GetFullPath(rootPath);
        }

        #endregion

        #region Public Methods

        public IConfiguration GetConfiguration(string? jsonFileName = null, string? sectionName = null)
        {
            if (string.IsNullOrEmpty(jsonFileName))
            {
                jsonFileName = DefaultConfigurationFileName;
            }

            var caller = Assembly.GetCallingAssembly(); // <- Must be in class that plugin is calling, otherwise returns host
            var jsonFilePath = GetPluginConfigFilePath(caller, RootPath, jsonFileName);

            if (!File.Exists(jsonFilePath))
            {
                return null;
            }

            if (_configurations.ContainsKey(jsonFilePath))
            {
                return GetConfigResult(_configurations[jsonFilePath], sectionName);
            }

            var basePath = jsonFilePath.GetDirectoryName();
            var config = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile(jsonFilePath)
                .Build();

            CacheConfiguration(jsonFilePath, config);

            var result = GetConfigResult(config, sectionName);
            return result;
        }

        public T? GetValue<T>(string name, T? defaultValue = default, string? sectionName = null)
        {
            var caller = Assembly.GetCallingAssembly(); // <- Must be in class that plugin is calling, otherwise returns host
            var jsonFilePath = GetPluginConfigFilePath(caller, RootPath);

            if (!_configurations.ContainsKey(jsonFilePath))
                return defaultValue;

            var config = _configurations[jsonFilePath];
            if (string.IsNullOrEmpty(sectionName))
            {
                return config.GetValue(name, defaultValue!);
            }

            var section = config.GetSection(sectionName);
            if (section == null)
                return defaultValue;

            var value = section.GetValue(name, defaultValue);
            return value;
        }

        #endregion

        #region Private Methods

        private void CacheConfiguration(string filePath, IConfiguration config)
        {
            if (!_configurations.ContainsKey(filePath))
            {
                _configurations.Add(filePath, config);
            }
            else
            {
                _configurations[filePath] = config;
            }
        }

        private static IConfiguration GetConfigResult(IConfiguration config, string? sectionName)
        {
            if (!string.IsNullOrEmpty(sectionName))
            {
                return config.GetSection(sectionName);
            }
            return config;
        }

        private static string GetPluginConfigFilePath(Assembly caller, string rootPath, string? jsonFileName = null)
        {
            var pluginFolder = caller.Location.GetDirectoryName();
            var basePluginFolder = Path.Combine(rootPath, pluginFolder!);
            if (string.IsNullOrEmpty(jsonFileName))
            {
                jsonFileName = DefaultConfigurationFileName;
            }
            var jsonFilePath = Path.Combine(basePluginFolder, jsonFileName);
            return jsonFilePath;
        }

        #endregion
    }
}