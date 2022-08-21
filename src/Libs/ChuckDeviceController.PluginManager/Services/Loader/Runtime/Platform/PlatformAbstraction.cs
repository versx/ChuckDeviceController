namespace ChuckDeviceController.PluginManager.Services.Loader.Runtime.Platform
{
    using System.Runtime.InteropServices;

    public static class PlatformAbstraction
    {
        public static bool IsWindows() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        public static bool IsMacOS() => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

        public static bool IsLinux() => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
    }
}