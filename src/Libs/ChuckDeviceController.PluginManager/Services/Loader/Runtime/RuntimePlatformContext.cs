namespace ChuckDeviceController.PluginManager.Services.Loader.Runtime;

using System.Runtime.InteropServices;

using ChuckDeviceController.PluginManager.Services.Loader.Runtime.Platform;

public class RuntimePlatformContext : IRuntimePlatformContext
{
    public RuntimePlatformContext()
    {
    }

    public IEnumerable<string> GetPlatformExtensions() => GetPlatformDependencyFileExtensions();

    public IEnumerable<string> GetPluginDependencyNames(string name) =>
        GetPluginDependencyFileExtensions()
            .Select(ext => $"{name}{ext}");

    public IEnumerable<string> GetPlatformDependencyNames(string name) =>
         GetPlatformDependencyFileCandidates(name);

    public RuntimeInfo GetRuntimeInfo()
    {
        var runtimes = new List<Runtime>();
        var runtimeBasePath = GetRuntimeBasePath();
        var platformIndependendPath = Path.GetFullPath(runtimeBasePath);

        var runtimeFolders = GetDirectories(platformIndependendPath);
        foreach (var runtimeFolder in runtimeFolders)
        {
            // Gets the directory name
            var runtimeName = Path.GetFileName(runtimeFolder);
            var runtimeType = ParseType(runtimeName);
            var runtimeVersions = GetDirectories(runtimeFolder);
            foreach (var runtimeVersionFolder in runtimeVersions)
            {
                // Gets the directory name
                var runtimeVersion = Path.GetFileName(runtimeVersionFolder);
                var runtimeLocation = runtimeVersionFolder;
                runtimes.Add(new Runtime
                {
                    Version = runtimeVersion,
                    Location = runtimeLocation,
                    RuntimeType = runtimeType,
                });
            }
        }

        return new RuntimeInfo
        {
            Runtimes = runtimes
        };
    }

    private static string GetRuntimeBasePath()
    {
        if (PlatformAbstraction.IsWindows())
            return "C:\\Program Files\\dotnet\\shared";
        if (PlatformAbstraction.IsMacOS())
            return "/usr/local/share/dotnet/shared";
        if (PlatformAbstraction.IsLinux())
            return "/usr/share/dotnet/shared";

        throw ThrowNotSupportedPlatformException();
    }

    private static RuntimeType ParseType(string runtimeName)
    {
        return runtimeName.ToUpper() switch
        {
            "MICROSOFT.ASPNETCORE.ALL" => RuntimeType.AspNetCoreAll,
            "MICROSOFT.ASPNETCORE.APP" => RuntimeType.AspNetCoreApp,
            "MICROSOFT.NETCORE.APP" => RuntimeType.NetCoreApp,
            "MICROSOFT.WINDOWSDESKTOP.APP" => RuntimeType.WindowsDesktopApp,
            _ => throw new Exception($"Runtime {runtimeName} could not be parsed"),
        };
    }

    private static string[] GetPluginDependencyFileExtensions()
    {
        return new[]
        {
            ".dll",
            ".ni.dll",
            ".exe",
            ".ni.exe",
        };
    }

    private static string[] GetPlatformDependencyFileCandidates(string name)
    {
        if (PlatformAbstraction.IsWindows())
        {
            return new[] { $"{name}.dll" };
        }

        if (PlatformAbstraction.IsMacOS())
        {
            return new[]
            {
                $"{name}.dylib",
                $"lib{name}.dylib"
            };
        }

        if (PlatformAbstraction.IsLinux())
        {
            return new[]
            {
                $"{name}.so",
                $"{name}.so.1",
                $"lib{name}.so",
                $"lib{name}.so.1",
            };
        }

        throw ThrowNotSupportedPlatformException();
    }

    private static string[] GetPlatformDependencyFileExtensions()
    {
        if (PlatformAbstraction.IsWindows())
            return new[] { ".dll" };

        if (PlatformAbstraction.IsMacOS())
            return new[] { ".dylib" };

        if (PlatformAbstraction.IsLinux())
            return new[] { ".so", ".so.1" };

        throw ThrowNotSupportedPlatformException();
    }

    private static Exception ThrowNotSupportedPlatformException()
    {
        return new Exception($"Platform {RuntimeInformation.OSDescription} is not supported");
    }

    private static IEnumerable<string> GetDirectories(string path, string searchPattern = "*")
    {
        var directories = Directory.GetDirectories(path, searchPattern, SearchOption.TopDirectoryOnly);
        return directories;
    }
}