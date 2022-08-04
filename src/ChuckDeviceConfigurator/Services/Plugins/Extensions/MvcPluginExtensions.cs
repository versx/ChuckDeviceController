namespace ChuckDeviceConfigurator.Services.Plugins.Extensions
{
    using Microsoft.AspNetCore.Mvc.ApplicationParts;

    using ChuckDeviceController.Plugins;

    // Credits: https://github.com/natemcmaster/DotNetCorePlugins/blob/main/src/Plugins.Mvc/MvcPluginExtensions.cs
    /// <summary>
    /// Extends the MVC builder.
    /// </summary>
    public static class MvcPluginExtensions
    {
        /// <summary>
        /// Loads controllers and razor pages from a plugin assembly.
        /// </summary>
        /// <param name="mvcBuilder">The MVC builder</param>
        /// <param name="assemblyFile">Full path the main .dll file for the plugin.</param>
        /// <returns>The builder</returns>
        public static IMvcBuilder AddPluginFromAssemblyFile(this IMvcBuilder mvcBuilder, string assemblyFile)
        {
            var pluginLoader = new PluginLoader<IPlugin>(assemblyFile);
            return mvcBuilder.AddPluginLoader(pluginLoader);
        }

        /// <summary>
        /// Loads controllers and razor pages from a plugin loader.
        /// <para>
        /// In order for this to work, the PluginLoader instance must be configured to share the types
        /// <see cref="ProvideApplicationPartFactoryAttribute" /> and <see cref="RelatedAssemblyAttribute" />
        /// (comes from Microsoft.AspNetCore.Mvc.Core.dll).
        /// </para>
        /// </summary>
        /// <param name="mvcBuilder">The MVC builder</param>
        /// <param name="pluginLoader">An instance of PluginLoader.</param>
        /// <returns>The builder</returns>
        public static IMvcBuilder AddPluginLoader(this IMvcBuilder mvcBuilder, PluginLoader<IPlugin> pluginLoader)
        {
            var pluginAssembly = pluginLoader.LoadDefaultAssembly();
            if (pluginAssembly == null)
            {
                Console.WriteLine($"Failed to load the default plugin assembly '{pluginLoader.PluginFilePath}'");
                return null;
            }

            // Loads MVC application parts from plugin assemblies
            var partFactory = ApplicationPartFactory.GetApplicationPartFactory(pluginAssembly);
            var assemblyParts = partFactory.GetApplicationParts(pluginAssembly);
            foreach (var part in assemblyParts)
            {
                mvcBuilder.PartManager.ApplicationParts.Add(part);
            }

            // Finds and loads related parts, such as MvcAppPlugin1.Views.dll.
            var relatedAssembliesAttrs = (RelatedAssemblyAttribute[])pluginAssembly.GetCustomAttributes(typeof(RelatedAssemblyAttribute), false);
            foreach (var attr in relatedAssembliesAttrs)
            {
                var assemblyFileName = attr.AssemblyFileName;
                var assembly = pluginLoader.LoadAssemblyFromPath(assemblyFileName);
                if (assembly == null)
                {
                    Console.WriteLine($"Assembly failed to load: {assemblyFileName}");
                    continue;
                }
                partFactory = ApplicationPartFactory.GetApplicationPartFactory(assembly);
                var otherParts = partFactory.GetApplicationParts(assembly);
                foreach (var part in otherParts)
                {
                    Console.WriteLine($"Part: {part.Name}");
                    mvcBuilder.PartManager.ApplicationParts.Add(part);
                }
            }

            return mvcBuilder;
        }
    }
}