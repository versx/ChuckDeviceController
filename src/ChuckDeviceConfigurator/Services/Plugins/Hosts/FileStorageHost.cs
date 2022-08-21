namespace ChuckDeviceConfigurator.Services.Plugins.Hosts
{
    using System.Reflection;

    using ChuckDeviceController.Extensions.Json;
    using ChuckDeviceController.Plugin;

    public class FileStorageHost : IFileStorageHost
    {
        #region Constants

        private const string GenericFileExt = ".dat";

        #endregion

        #region Variables

        private static readonly ILogger<IFileStorageHost> _logger =
            new Logger<IFileStorageHost>(LoggerFactory.Create(x => x.AddConsole()));
        private readonly Dictionary<string, object> _configurations = new();

        #endregion

        #region Properties

        public string RootPath { get; }

        #endregion

        #region Constructors

        public FileStorageHost()
            : this(Strings.PluginsFolder)
        {
        }

        public FileStorageHost(string rootPath)
        {
            if (string.IsNullOrEmpty(rootPath))
            {
                throw new ArgumentNullException(nameof(rootPath));
            }

            if (!Directory.Exists(rootPath))
            {
                throw new ArgumentException("Root path does not exists", nameof(rootPath));
            }

            // Should be default root plugins folder
            RootPath = rootPath;
        }

        #endregion

        #region Public Methods

        public T Load<T>(string location, string name)
        {
            var callingAssembly = Assembly.GetCallingAssembly();
            var basePath = GetPluginRootFolder(callingAssembly, location);
            CreateDirectory(basePath);

            var dataFile = Path.Combine(basePath, AppendGenericExt(name));
            if (!File.Exists(dataFile))
            {
                return default;
            }

            try
            {
                var data = File.ReadAllText(dataFile);
                if (data == null)
                {
                    // File is empty
                    return default;
                }

                var obj = data.FromJson<T>();
                if (obj == null)
                {
                    // Failed to deserialize file to type
                    return default;
                }

                // Cache storage file data
                CacheFileData(dataFile, obj);

                return obj;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Load");
            }

            return default;
        }

        public bool Save<T>(T data, string location, string name)
        {
            var callingAssembly = Assembly.GetCallingAssembly();
            var basePath = GetPluginRootFolder(callingAssembly, location);
            CreateDirectory(basePath);

            var dataFile = Path.Combine(basePath, AppendGenericExt(name));
            var tempCopy = Path.ChangeExtension(dataFile, ".tmp");

            try
            {
                if (File.Exists(dataFile))
                {
                    File.Move(dataFile, tempCopy, true);
                }

                var json = data.ToJson();
                File.WriteAllText(dataFile, json);

                if (File.Exists(tempCopy))
                {
                    File.Delete(tempCopy);
                }

                // Cache storage file data
                CacheFileData(dataFile, data);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Save");

                if (File.Exists(tempCopy))
                {
                    File.Move(tempCopy, dataFile, true);
                }
            }

            return false;
        }

        #endregion

        #region Helper Methods

        private void CacheFileData<T>(string filePath, T data)
        {
            if (data == null)
            {
                // Skip caching empty data
                return;
            }

            if (!_configurations.ContainsKey(filePath))
            {
                _configurations.Add(filePath, data);
            }
            else
            {
                _configurations[filePath] = data;
            }
        }

        private string GetPluginRootFolder(Assembly caller, string location)
        {
            var pluginFolder = Path.GetDirectoryName(caller.Location);
            var basePath = Path.Combine(RootPath, pluginFolder!, location);
            return basePath;
        }

        private static void CreateDirectory(string folderPath)
        {
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
        }

        private static string AppendGenericExt(string fileName)
        {
            // Ensure that we always save or load data from a file with an extension
            if (!Path.HasExtension(fileName))
            {
                // Append generic '.dat' extension if file has no extension at all
                fileName += GenericFileExt;
            }
            return fileName;
        }

        #endregion
    }
}