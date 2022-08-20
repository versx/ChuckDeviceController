namespace ChuckDeviceController.Plugin.Services
{
    /// <summary>
    /// Register services from a separate class, aka 'ConfigureServices'
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class PluginBootstrapperAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the plugin contract type.
        /// </summary>
        public Type PluginType { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pluginType"></param>
        public PluginBootstrapperAttribute(Type pluginType)
        {
            PluginType = pluginType;
        }
    }
}