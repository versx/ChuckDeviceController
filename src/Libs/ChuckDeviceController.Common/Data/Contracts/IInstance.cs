namespace ChuckDeviceController.Common.Data.Contracts
{
    public interface IInstance : IBaseEntity
    {
        string Name { get; }

        InstanceType Type { get; }

        ushort MinimumLevel { get; }

        ushort MaximumLevel { get; }

        List<string> Geofences { get; }

        InstanceData? Data { get; }
    }
}