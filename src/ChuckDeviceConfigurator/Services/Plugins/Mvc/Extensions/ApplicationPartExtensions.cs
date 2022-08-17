namespace ChuckDeviceConfigurator.Services.Plugins.Mvc.Extensions
{
    using System.Reflection;

    using Microsoft.AspNetCore.Mvc.ApplicationParts;

    public static class ApplicationPartExtensions
    {
        public static void AddApplicationPart(this IMvcBuilder mvcBuilder, Assembly assembly)
        {
            // Load assembly as AssemblyPart for Mvc controllers
            var part = new AssemblyPart(assembly);

            // Add loaded assembly as application part
            mvcBuilder.PartManager.ApplicationParts.Add(part);
        }
    }
}