namespace ChuckDeviceController.Plugin
{
    /// <summary>
    /// Determines where the static files (i.e. 'wwwroot') will be located to the plugin.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class StaticFilesLocationAttribute : Attribute
    {
        /// <summary>
        /// Gets the location of the static files.
        /// </summary>
        public StaticFilesLocation Location { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="location"></param>
        public StaticFilesLocationAttribute(StaticFilesLocation location)
        {
            Location = location;
        }
    }
}