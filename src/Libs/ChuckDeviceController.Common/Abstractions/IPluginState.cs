namespace ChuckDeviceController.Common.Abstractions;

public interface IPluginState : IBaseEntity
{
    string Name { get; }

    string FullPath { get; }

    PluginState State { get; }
}