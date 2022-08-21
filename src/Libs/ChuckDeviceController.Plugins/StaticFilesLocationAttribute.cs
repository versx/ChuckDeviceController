namespace ChuckDeviceController.Plugin
{
    /// <summary>
    /// 
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class StaticFilesLocationAttribute : Attribute
    {
        /// <summary>
        /// 
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