namespace ChuckDeviceController.PluginManager.Services.Loader.Dependencies
{
    public class PluginDependencyContext : IPluginDependencyContext
    {
        #region Properties

        public string AssemblyFullPath { get; set; }

        public IEnumerable<HostDependency> HostDependencies { get; set; }

        public IEnumerable<RemoteDependency> RemoteDependencies { get; set; }

        public IEnumerable<PluginDependency> PluginDependencies { get; set; }

        public IEnumerable<PluginResourceDependency> PluginResourceDependencies { get; set; }

        public IEnumerable<PluginDependency> PluginReferenceDependencies { get; set; }

        public IEnumerable<PlatformDependency> PlatformDependencies { get; set; }

        public IEnumerable<string> AdditionalProbingPaths { get; set; }

        #endregion

        #region Constructor

        internal PluginDependencyContext(
            string assemblyFullPath,
            IEnumerable<HostDependency> hostDependencies,
            IEnumerable<RemoteDependency> remoteDependencies,
            IEnumerable<PluginDependency> pluginDependencies,
            IEnumerable<PluginResourceDependency> pluginResourceDependencies,
            IEnumerable<PluginDependency> pluginReferenceDependencies,
            IEnumerable<PlatformDependency> platformDependencies,
            IEnumerable<string> additionalProbingPaths)
        {
            AssemblyFullPath = assemblyFullPath;
            HostDependencies = hostDependencies;
            RemoteDependencies = remoteDependencies;
            PluginDependencies = pluginDependencies;
            PluginResourceDependencies = pluginResourceDependencies;
            PluginReferenceDependencies = pluginReferenceDependencies;
            PlatformDependencies = platformDependencies;
            AdditionalProbingPaths = additionalProbingPaths ?? Enumerable.Empty<string>();
        }

        #endregion

        #region IDisposable Implementation

        private bool _disposed = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                AssemblyFullPath = null;
                HostDependencies = null;
                RemoteDependencies = null;
                PluginDependencies = null;
                PluginResourceDependencies = null;
                PluginReferenceDependencies = null;
                PlatformDependencies = null;
                AdditionalProbingPaths = null;
            }
            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}