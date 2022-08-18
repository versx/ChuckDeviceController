namespace ChuckDeviceController.PluginManager.FileProviders
{
    using System.Collections.Concurrent;
    using System.Reflection;

    using Microsoft.Extensions.FileProviders;
    using Microsoft.Extensions.Primitives;

    public class DefaultPluginViewsAssemblyFileProvider : IFileProvider
    {
        protected readonly PhysicalFileProvider _webRootFileProvider;
        protected readonly string _pathToPlugins;

        public DefaultPluginViewsAssemblyFileProvider(string hostingRootPath, string pathToPlugins)
        {
            if (!Path.IsPathRooted(pathToPlugins))
                throw new ArgumentException($"{nameof(pathToPlugins)} must be rooted (absolute path).");

            _pathToPlugins = pathToPlugins;
            _webRootFileProvider = new PhysicalFileProvider(hostingRootPath);
        }

        private static IPluginCache GetLoadedPluginsCache()
        {
            return DefaultStaticPluginCacheAccessor.CurrentCache;
        }

        private IFileProvider GetPluginFileProvider(string subFolder)
        {
            var cache = GetLoadedPluginsCache();
            if (cache == null)
                return null;

            foreach (var loadedPlugin in cache.GetAll())
            {
                var pluginAssemblyName = loadedPlugin.AssemblyShim.GetName().Name;
                var pathToPlugin = Path.Combine(_pathToPlugins, pluginAssemblyName);
                var pathCandidate = Path.Combine(pathToPlugin, SanitizeSubPath(subFolder));
                if (File.Exists(pathCandidate))
                {
                    return new PhysicalFileProvider(pathToPlugin);
                }
            }
            return null;
        }

        private static string SanitizeSubPath(string subFolder)
        {
            if (subFolder.StartsWith('/'))
            {
                return subFolder[1..];
            }
            return subFolder;
        }

        public IDirectoryContents GetDirectoryContents(string subFolder)
        {
            var pluginFileProvider = GetPluginFileProvider(subFolder);
            if (pluginFileProvider != null)
            {
                return pluginFileProvider.GetDirectoryContents(subFolder);
            }
            return _webRootFileProvider.GetDirectoryContents(subFolder);
        }

        public IFileInfo GetFileInfo(string subFolder)
        {
            var pluginFileProvider = GetPluginFileProvider(subFolder);
            if (pluginFileProvider != null)
            {
                return pluginFileProvider.GetFileInfo(subFolder);
            }
            return _webRootFileProvider.GetFileInfo(subFolder);
        }

        public IChangeToken Watch(string filter)
        {
            return _webRootFileProvider.Watch(filter);
        }
    }

    public interface IPluginCacheAccessorBootstrapper
    {
        void SetCurrentCache(IPluginCache cache);
    }

    public class DefaultStaticPluginCacheAccessorBootstrapper : IPluginCacheAccessorBootstrapper
    {
        protected bool _isBootstrapped;

        public DefaultStaticPluginCacheAccessorBootstrapper(IPluginCache cache)
        {
            if (_isBootstrapped)
            {
                throw new NotSupportedException($"IPluginCache was already bootstrapped");
            }

            SetCurrentCache(cache);
            _isBootstrapped = true;
        }

        public void SetCurrentCache(IPluginCache cache)
        {
            DefaultStaticPluginCacheAccessor.CurrentCache = cache;
        }
    }

    public static class DefaultStaticPluginCacheAccessor
    {
        public static IPluginCache CurrentCache { get; internal set; }
    }

    public interface IPluginCache
    {
        void Add(Assembly pluginAssembly, IEnumerable<Type>? hostTypes = null);

        void Remove(string assemblyName);

        ICachedPluginAssembly[] GetAll();
    }

    public class DefaultScopedPluginCache : IPluginCache
    {
        protected ConcurrentBag<ICachedPluginAssembly> _pluginCache;

        public DefaultScopedPluginCache()
        {
            _pluginCache = new ConcurrentBag<ICachedPluginAssembly>();
        }

        public void Add(Assembly pluginAssembly, IEnumerable<Type>? hostTypes = null)
        {
            _pluginCache.Add(new CachedPluginAssembly
            {
                AssemblyShim = pluginAssembly,
                HostTypes = hostTypes
            });
        }

        public void Remove(string assemblyName)
        {
            _pluginCache = new ConcurrentBag<ICachedPluginAssembly>(_pluginCache.Where(a => a.AssemblyShim.GetName().Name != assemblyName));
        }

        public ICachedPluginAssembly[] GetAll() => _pluginCache.ToArray();
    }

    public interface ICachedPluginAssembly
    {
        Assembly AssemblyShim { get; }

        IEnumerable<Type>? HostTypes { get; }
    }

    public class CachedPluginAssembly : ICachedPluginAssembly
    {
        public Assembly AssemblyShim { get; set; }

        public IEnumerable<Type> HostTypes { get; set; }
    }
}