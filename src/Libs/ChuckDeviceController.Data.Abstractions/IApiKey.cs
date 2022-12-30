namespace ChuckDeviceController.Data.Abstractions;

using ChuckDeviceController.Data.Common;

public interface IApiKey
{
    uint Id { get; }

    string Name { get; }

    string Key { get; }

    PluginApiKeyScope Scope { get; }

    ulong ExpirationTimestamp { get; }

    bool IsEnabled { get; }
}