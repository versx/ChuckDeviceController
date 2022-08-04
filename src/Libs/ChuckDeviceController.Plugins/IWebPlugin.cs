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
        void Configure(IApplicationBuilder appBuilder);

        void ConfigureServices(IServiceCollection services);
    }
}