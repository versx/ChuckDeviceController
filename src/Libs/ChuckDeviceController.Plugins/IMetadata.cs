namespace ChuckDeviceController.Plugins
{
    /// <summary>
    /// Plugin metadata details.
    /// </summary>
    public interface IMetadata
    {
        /// <summary>
        /// Gets or sets the name of the Plugin.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets or sets the description about the Plugin.
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Gets or sets the creator/author name that wrote the Plugin.
        /// </summary>
        string Author { get; }

        /// <summary>
        /// Gets or sets the current version of the Plugin.
        /// </summary>
        Version Version { get; }
    }
}