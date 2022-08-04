namespace ChuckDeviceController.Plugins
{
    /// <summary>
    /// Metadata details relating to the Plugin
    /// </summary>
    public interface IMetadata
    {
        /// <summary>
        /// Gets the name of the Plugin.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the description about the Plugin.
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Gets the creator/author name that wrote the Plugin.
        /// </summary>
        string Author { get; }

        /// <summary>
        /// Gets the current version of the Plugin.
        /// </summary>
        Version Version { get; }
    }
}