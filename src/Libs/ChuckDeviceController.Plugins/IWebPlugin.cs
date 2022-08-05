namespace ChuckDeviceController.Plugins
{
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// Interface contract allowing Mvc services registration and configuration
    /// </summary>
    public interface IWebPlugin
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="appBuilder"></param>
        void Configure(IApplicationBuilder appBuilder);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        void ConfigureServices(IServiceCollection services);
    }
}