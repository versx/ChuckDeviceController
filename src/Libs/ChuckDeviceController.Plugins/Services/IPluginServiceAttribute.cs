namespace ChuckDeviceController.Plugins.Services
{
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// Contract for registering plugin service classes marked with
    /// 'PluginServiceAttribute' with the host application.
    /// </summary>
    public interface IPluginServiceAttribute
    {
        /// <summary>
        /// Gets or sets the Service contract type.
        /// </summary>
        Type ServiceType { get; }

        /// <summary>
        /// Gets or sets the service implementation type.
        /// </summary>
        Type ProxyType { get; }

        /// <summary>
        /// Gets or sets who provided the service.
        /// </summary>
        PluginServiceProvider ProvidedBy { get; }

        /// <summary>
        /// Gets or sets the service lifetime for the plugin service.
        /// </summary>
        ServiceLifetime Lifetime { get; }
    }
}