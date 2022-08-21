namespace ChuckDeviceController.PluginManager.Services.Loader.Dependencies
{
    public interface IPluginDependencyContext : IDisposable
    {
        string AssemblyFullPath { get; }

        /// <summary>
        /// Host dependencies are detected automatically by reading out the deps.json file
        /// </summary>
        /// <value></value>
        IEnumerable<HostDependency> HostDependencies { get; }

        /// <summary>
        /// Remote dependencies are specified manually via the AddRemoteType builder
        /// </summary>
        /// <value></value>
        IEnumerable<RemoteDependency> RemoteDependencies { get; }

        /// <summary>
        /// Plugin dependencies are detected automatically by reading out the deps.json file
        /// </summary>
        /// <value></value>
        IEnumerable<PluginDependency> PluginDependencies { get; }

        /// <summary>
        /// 
        /// </summary>
        IEnumerable<PluginResourceDependency> PluginResourceDependencies { get; }

        /// <summary>
        /// 
        /// </summary>
        IEnumerable<PlatformDependency> PlatformDependencies { get; }

        /// <summary>
        /// 
        /// </summary>
        IEnumerable<string> AdditionalProbingPaths { get; }
    }
}