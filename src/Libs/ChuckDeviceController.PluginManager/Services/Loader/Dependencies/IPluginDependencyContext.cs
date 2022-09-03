namespace ChuckDeviceController.PluginManager.Services.Loader.Dependencies
{
    public interface IPluginDependencyContext : IDisposable
    {
        /// <summary>
        /// Gets or sets the full file path of the assembly.
        /// </summary>
        string AssemblyFullPath { get; }

        /// <summary>
        /// Host dependencies are detected automatically by reading
        /// out the deps.json file.
        /// </summary>
        IEnumerable<HostDependency> HostDependencies { get; }

        /// <summary>
        /// Remote dependencies are specified manually via the
        /// AddRemoteType builder.
        /// </summary>
        IEnumerable<RemoteDependency> RemoteDependencies { get; }

        /// <summary>
        /// Plugin dependencies are detected automatically by reading
        /// out the deps.json file.
        /// </summary>
        IEnumerable<PluginDependency> PluginDependencies { get; }

        /// <summary>
        /// Plugin resource dependency collection.
        /// </summary>
        IEnumerable<PluginResourceDependency> PluginResourceDependencies { get; }

        /// <summary>
        /// Plugin reference dependency collection.
        /// </summary>
        IEnumerable<PluginDependency> PluginReferenceDependencies { get; }

        /// <summary>
        /// Plugin platform dependency collection.
        /// </summary>
        IEnumerable<PlatformDependency> PlatformDependencies { get; }

        /// <summary>
        /// Additional paths used when discovering dependencies.
        /// </summary>
        IEnumerable<string> AdditionalProbingPaths { get; }
    }
}