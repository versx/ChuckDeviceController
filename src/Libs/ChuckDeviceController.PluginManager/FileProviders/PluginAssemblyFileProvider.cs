namespace ChuckDeviceController.PluginManager.FileProviders
{
    using System.Reflection;

    using Microsoft.Extensions.FileProviders;

    public class PluginAssemblyFileProvider : PhysicalFileProvider
    {
        public PluginAssemblyFileProvider(string pluginAssemblyPath)
            : base(GetDirectoryName(pluginAssemblyPath))
        {
        }

        public PluginAssemblyFileProvider(Assembly assembly)
            : base(GetDirectoryName(assembly.Location))
        {
        }

        private static string? GetDirectoryName(string filePath)
        {
            return Path.GetDirectoryName(filePath);
        }
    }
}