namespace ChuckDeviceConfigurator.ViewModels;

using System.ComponentModel;

using ChuckDeviceController.Data.Entities;

public class InstanceDetailsViewModel : Instance
{
    [DisplayName("Assigned Devices")]
    public List<Device> Devices { get; set; } = new();
}