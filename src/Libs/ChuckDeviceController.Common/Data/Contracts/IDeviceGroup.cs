namespace ChuckDeviceController.Common.Data.Contracts
{
    public interface IDeviceGroup : IBaseEntity
    {
        string Name { get; }

        IList<string> DeviceUuids { get; }
    }
}