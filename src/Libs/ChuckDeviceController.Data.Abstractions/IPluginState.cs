namespace ChuckDeviceController.Data.Abstractions;

using ChuckDeviceController.Data.Common;

public interface IPluginState : IBaseEntity
{
    string Name { get; }

    string FullPath { get; }

    PluginState State { get; }
}