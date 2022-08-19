namespace ChuckDeviceController.PluginManager.Mvc.Extensions
{
    using System.Reflection;

    using Microsoft.Extensions.DependencyInjection;

    using ChuckDeviceController.PluginManager.Mvc.ApplicationParts;

    public static class MvcBuilderExtensions
    {
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
                // Check that the service isn't already registered with
                // the service collection 
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
    }
}