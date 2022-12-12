namespace ChuckDeviceController.Common.Data.Contracts
{
    public interface IPluginState : IBaseEntity
    {
        string Name { get; }

        string FullPath { get; }

        PluginState State { get; }
    }
}