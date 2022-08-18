namespace ChuckDeviceController.PluginManager.Mvc.Extensions
{
    using System.Reflection;

    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.ApplicationParts;
    using Microsoft.AspNetCore.Mvc.Controllers;
    using Microsoft.AspNetCore.Mvc.Infrastructure;
    using Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Microsoft.Extensions.Primitives;

    using ChuckDeviceController.PluginManager.FileProviders;
    using ChuckDeviceController.PluginManager.Loader;
    using ChuckDeviceController.PluginManager.Mvc.ApplicationParts;
    using ChuckDeviceController.PluginManager.Services.Finder;
    using ChuckDeviceController.PluginManager.Services.Loader;
    using ChuckDeviceController.Plugins;

    public static class MvcBuilderExtensions
    {
        public const string ViewsFileExtension = ".Views.dll";

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mvcBuilder"></param>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IMvcBuilder AddServices(this IMvcBuilder mvcBuilder, IServiceCollection services)
        {
            // Register all available host services with plugin
            var servicesBuilder = new List<ServiceDescriptor>(services);
            foreach (var service in servicesBuilder)
            {
                if (!mvcBuilder.Services.Contains(service))
                {
                    mvcBuilder.Services.Add(service);
                }
            }
            return mvcBuilder;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mvcBuilder"></param>
        /// <param name="assembly"></param>
        /// <returns></returns>
        public static IMvcBuilder AddApplicationPart(this IMvcBuilder mvcBuilder, Assembly assembly)
        {
            // Load assembly as PluginAssemblyPart for Mvc controllers
            var part = new PluginAssemblyPart(assembly);

            // Add loaded assembly as application part
            mvcBuilder.PartManager.ApplicationParts.Add(part);
            return mvcBuilder;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddChuckMvc(this IServiceCollection services)
        {
            return services
                .AddSingleton<IPluginCache, DefaultScopedPluginCache>();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        /// <param name="webRootPath"></param>
        /// <param name="pathToPlugins"></param>
        /// <returns></returns>
        public static IServiceCollection AddChuckRazorPlugins(this IServiceCollection services, string webRootPath, string pathToPlugins)
        {
            return services
                .AddChuckMvc()
                .ConfigureRazorServices(webRootPath, pathToPlugins);
        }

        private static IServiceCollection ConfigureRazorServices(this IServiceCollection services, string webRootPath, string pathToPlugins)
        {
            return services
                .Configure<MvcRazorRuntimeCompilationOptions>(options =>
                {
                    options.FileProviders.Add(new DefaultPluginViewsAssemblyFileProvider(webRootPath, pathToPlugins));
                })
                .AddSingleton<IPluginCacheAccessorBootstrapper, DefaultStaticPluginCacheAccessorBootstrapper>();
        }

        public static IMvcBuilder ConfigureCompiledViews(this IMvcBuilder mvcBuilder, IPluginManager pluginManager)
        {
            // configure Compiled Views
            foreach (var (pluginName, pluginHost) in pluginManager.Plugins)
            {
                var pluginAssembly = pluginHost.PluginFinderResult.Assembly;
                var primaryModule = string.IsNullOrEmpty(pluginAssembly.Location)
                    ? pluginAssembly?.CodeBase
                    : pluginAssembly?.Location;
                var compiledViewAssembly = Path.ChangeExtension(primaryModule, ViewsFileExtension);

                if (!File.Exists(compiledViewAssembly))
                    continue;

                var compiledViews = Assembly.LoadFrom(compiledViewAssembly);
                mvcBuilder.ConfigureApplicationPartManager(options =>
                {
                    var applicationParts = new CompiledRazorAssemblyApplicationPartFactory().GetApplicationParts(compiledViews);
                    foreach (var part in applicationParts)
                    {
                        options.ApplicationParts.Add(part);
                    }
                });
            }
            return mvcBuilder;
        }
    }
}