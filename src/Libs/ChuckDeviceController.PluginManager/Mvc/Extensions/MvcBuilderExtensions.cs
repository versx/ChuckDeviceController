namespace ChuckDeviceController.PluginManager.Mvc.Extensions
{
    using Microsoft.Extensions.DependencyInjection;

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
    }
}