namespace ChuckDeviceController;

using System.Reflection;

public static class Strings
{
    private static readonly AssemblyName StrongAssemblyName = Assembly.GetExecutingAssembly().GetName();

    // File assembly details
    public static readonly string AssemblyName = StrongAssemblyName?.Name ?? "ChuckDeviceController";
    public static readonly string AssemblyVersion = StrongAssemblyName?.Version?.ToString() ?? "v1.0.0";
}