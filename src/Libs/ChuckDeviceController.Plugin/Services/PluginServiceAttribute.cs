namespace ChuckDeviceController.Plugin.Services
{
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// Registers plugin service classes that are marked with the
    /// 'PluginService' attribute with the host application in 
    /// order to be used with dependency injection.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class PluginServiceAttribute : Attribute, IPluginServiceAttribute
    {
        /// <summary>
        /// Gets or sets the service contract type.
        /// </summary>
        public Type ServiceType { get; set; }

        /// <summary>
        /// Gets or sets the service implementation type.
        /// </summary>
        public Type ProxyType { get; set; }

        /// <summary>
        /// Gets or sets who provided the service.
        /// </summary>
        public PluginServiceProvider Provider { get; set; }

        /// <summary>
        /// Gets or sets the service lifetime for the plugin service.
        /// </summary>
        public ServiceLifetime Lifetime { get; set; }

        /// <summary>
        /// 
        /// </summary>
        //public bool IsHostedService { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public PluginServiceAttribute()
        {
            ServiceType = typeof(Type);
            ProxyType = typeof(Type);
            Provider = PluginServiceProvider.Plugin;
            Lifetime = ServiceLifetime.Singleton;
            //IsHostedService = false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="serviceType"></param>
        /// <param name="proxyType"></param>
        /// <param name="provider"></param>
        /// <param name="lifetime"></param>
        public PluginServiceAttribute(
            Type serviceType,
            Type proxyType,
            PluginServiceProvider provider = PluginServiceProvider.Plugin,
            ServiceLifetime lifetime = ServiceLifetime.Singleton)
            //bool isHostedService = false)
        {
            ServiceType = serviceType;
            ProxyType = proxyType;
            Provider = provider;
            Lifetime = lifetime;
            //IsHostedService = isHostedService;
        }
    }
}