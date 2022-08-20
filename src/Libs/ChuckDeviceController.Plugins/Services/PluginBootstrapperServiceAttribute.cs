namespace ChuckDeviceController.Plugin.Services
{
    /// <summary>
    /// Assigns fields and properties in a plugin assembly with registered
    /// service implementations.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public class PluginBootstrapperServiceAttribute : Attribute, IPluginBootstrapperServiceAttribute
    {
        /// <summary>
        /// Gets or sets the bootstrap service contract type.
        /// </summary>
        public Type ServiceType { get; set; }

        /// <summary>
        /// Gets or sets the bootstrap service implementation type.
        /// </summary>
        public Type ProxyType { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="serviceType"></param>
        public PluginBootstrapperServiceAttribute(Type serviceType)
        {
            ServiceType = serviceType;
            ProxyType = typeof(Type);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="serviceType"></param>
        /// <param name="proxyType"></param>
        public PluginBootstrapperServiceAttribute(Type serviceType, Type proxyType)
        {
            ServiceType = serviceType;
            ProxyType = proxyType;
        }
    }
}