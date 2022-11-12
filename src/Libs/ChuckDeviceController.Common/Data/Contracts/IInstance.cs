namespace ChuckDeviceController.Common.Data.Contracts
{
    public interface IInstance : IBaseEntity
    {
        string Name { get; }

        InstanceType Type { get; }

        ushort MinimumLevel { get; }

        ushort MaximumLevel { get; }

        List<string> Geofences { get; }

        IInstanceData Data { get; }
    }

    public interface IInstanceData //: IDictionary<string, object>
    {
        string? CustomInstanceType { get; }

        string? AccountGroup { get; }

        bool? IsEvent { get; }
    }
}