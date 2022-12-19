namespace ChuckDeviceController.Data.Abstractions;

public interface IAssignment : IBaseEntity
{
    uint Id { get; }

    string InstanceName { get; }

    string? SourceInstanceName { get; }

    string? DeviceUuid { get; }

    uint Time { get; }

    DateTime? Date { get; }

    string? DeviceGroupName { get; }

    public bool Enabled { get; }
}