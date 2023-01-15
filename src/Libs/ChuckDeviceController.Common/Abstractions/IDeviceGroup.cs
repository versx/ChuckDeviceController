namespace ChuckDeviceController.Common.Abstractions;

public interface IDeviceGroup : IBaseEntity
{
    string Name { get; }

    List<string> DeviceUuids { get; }
}