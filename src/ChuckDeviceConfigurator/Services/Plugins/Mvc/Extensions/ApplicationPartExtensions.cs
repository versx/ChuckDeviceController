namespace ChuckDeviceConfigurator.Services.Plugins.Mvc.Extensions
{
    using System.Reflection;

    using Microsoft.AspNetCore.Mvc.ApplicationParts;

    using ChuckDeviceConfigurator.Services.Plugins.Mvc.ApplicationParts;

    public static class ApplicationPartExtensions
    {
        public const string ViewsFileExtension = ".Views.dll";

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mvcBuilder"></param>
        /// <param name="assembly"></param>
        public static void AddApplicationPart(this IMvcBuilder mvcBuilder, Assembly assembly)
        {
            // Load assembly as PluginAssemblyPart for Mvc controllers
            var part = new PluginAssemblyPart(assembly);

            // Add loaded assembly as application part
            mvcBuilder.PartManager.ApplicationParts.Add(part);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mvcBuilder"></param>
        /// <param name="pluginManager"></param>
        public static void ConfigureCompiledViews(this IMvcBuilder mvcBuilder, IPluginManager pluginManager)
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
        }
    }
}