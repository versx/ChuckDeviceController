namespace ChuckDeviceController.PluginManager.Services.Loader.Dependencies
{
    using System.Reflection;

    public class RemoteDependency
    {
        public AssemblyName DependencyName { get; set; }

        public Version Version => DependencyName?.Version;
    }
}