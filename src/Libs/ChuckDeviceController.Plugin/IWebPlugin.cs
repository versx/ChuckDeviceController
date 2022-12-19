namespace ChuckDeviceController.Plugin;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Interface contract allowing Mvc services registration and configuration
/// </summary>
public interface IWebPlugin
{
    /// <summary>
    /// Configures the application to set up middlewares, map routing rules, etc.
    /// </summary>
    /// <param name="appBuilder">
    /// Provides the mechanisms to configure an application's request pipeline.
    /// </param>
    void Configure(WebApplication appBuilder);

    /// <summary>
    /// Register services into the IServiceCollection to use with Dependency Injection.
    /// </summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    void ConfigureServices(IServiceCollection services);

    /// <summary>
    /// Provides an opportunity for plugins to configure Mvc Builder.
    /// </summary>
    /// <param name="mvcBuilder">IMvcBuilder instance that can be configured.</param>
    void ConfigureMvcBuilder(IMvcBuilder mvcBuilder);
}