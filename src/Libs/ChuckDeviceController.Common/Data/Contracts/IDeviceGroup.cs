namespace ChuckDeviceController.Common.Data.Contracts
{
    public interface IDeviceGroup : IBaseEntity
    {
        string Name { get; }

        List<string> DeviceUuids { get; }
    }
}