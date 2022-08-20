namespace ChuckDeviceController.PluginManager.FileProviders
{
    using System.Reflection;

    using Microsoft.Extensions.FileProviders;

    using ChuckDeviceController.PluginManager.Extensions;

    public class PluginAssemblyFileProvider : PhysicalFileProvider
    {
        public PluginAssemblyFileProvider(string pluginAssemblyPath)
            : base(pluginAssemblyPath.GetDirectoryName())
        {
        }

        public PluginAssemblyFileProvider(Assembly assembly)
            : base(assembly.Location.GetDirectoryName())
        {
        }
    }
}