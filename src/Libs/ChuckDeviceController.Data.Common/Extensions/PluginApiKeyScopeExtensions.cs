namespace ChuckDeviceController.Data.Common.Extensions;

public static class PluginApiKeyScopeExtensions
{
    public static PluginApiKeyScope SetFlag(this PluginApiKeyScope flags, PluginApiKeyScope flag)
    {
        return flags | flag;
    }

    public static PluginApiKeyScope UnsetFlag(this PluginApiKeyScope flags, PluginApiKeyScope flag)
    {
        return flags & (~flag);
    }

    public static bool HasFlag(this PluginApiKeyScope flags, PluginApiKeyScope flag)
    {
        // Works with 'None/0'

        return (flags & flag) == flag;
    }

    public static PluginApiKeyScope ToogleFlag(this PluginApiKeyScope flags, PluginApiKeyScope flag)
    {
        return flags ^ flag;
    }
}