namespace ChuckDeviceController.Plugins.Services
{
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// Register services from a separate class, aka 'ConfigureServices'
    /// </summary>
    public interface IPluginBootstrapper
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        IServiceCollection Bootstrap(IServiceCollection services);
    }
}