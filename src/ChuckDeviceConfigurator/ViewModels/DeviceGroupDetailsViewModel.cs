namespace ChuckDeviceConfigurator.ViewModels;

using System.ComponentModel;

using ChuckDeviceController.Data.Entities;

public class DeviceGroupDetailsViewModel : DeviceGroup
{
    [DisplayName("Devices")]
    public List<Device> Devices { get; set; } = new();
}