namespace ChuckDeviceController.PluginManager.Services.Loader.Runtime;

public class PluginPlatformVersion
{
    public string Version { get; private set; } = null!;

    public RuntimeType Runtime { get; private set; }

    public bool IsSpecified => !string.IsNullOrEmpty(Version);

    public static PluginPlatformVersion Create(string version, RuntimeType runtime = RuntimeType.None) => new()
    {
        Version = version,
        Runtime = runtime
    };

    public static PluginPlatformVersion Empty() => new();
}