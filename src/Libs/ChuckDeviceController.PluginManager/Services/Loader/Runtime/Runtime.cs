namespace ChuckDeviceController.PluginManager.Services.Loader.Runtime
{
    /// <summary>
    /// Runtime information.
    /// </summary>
    public class Runtime
    {
        /// <summary>
        /// Gets or sets the runtime type.
        /// </summary>
        public RuntimeType RuntimeType { get; set; }

        /// <summary>
        /// Gets or sets the runtime version.
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Gets or sets the runtime version folder path.
        /// </summary>
        public string Location { get; set; }
    }
}