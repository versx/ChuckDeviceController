namespace ChuckDeviceController.PluginManager.Extensions
{
    using System.Reflection;
    using System.Runtime.Versioning;

    public static class AssemblyExtensions
    {
        public static string? GetHostFramework()
        {
            var assembly = Assembly.GetEntryAssembly();
            return assembly?.GetHostFramework();
        }

        public static string? GetHostFramework(this Assembly assembly)
        {
            var attr = assembly?.GetCustomAttribute<TargetFrameworkAttribute>();
            var hostFramework = attr?.FrameworkName;
            return hostFramework;
        }
    }
}