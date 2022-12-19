namespace ChuckDeviceController.PluginManager.FileProviders;

using System.Reflection;

using Microsoft.Extensions.FileProviders;

using ChuckDeviceController.PluginManager.Extensions;

public class PluginPhysicalFileProvider : PhysicalFileProvider
{
    public PluginPhysicalFileProvider(Assembly assembly, string webRoot)
        : base(Path.Combine(assembly.Location.GetDirectoryName()!, webRoot))
    {
    }
}