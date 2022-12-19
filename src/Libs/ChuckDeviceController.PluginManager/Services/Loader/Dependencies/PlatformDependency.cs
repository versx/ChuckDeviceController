namespace ChuckDeviceController.PluginManager.Services.Loader.Dependencies;

public class PlatformDependency
{
    public string DependencyNameWithoutExtension { get; set; } = null!;

    public Version Version { get; set; } = null!;

    public string DependencyPath { get; set; } = null!;

    public string ProbingPath { get; set; } = null!;
}