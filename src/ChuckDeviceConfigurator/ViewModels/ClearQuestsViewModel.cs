namespace ChuckDeviceConfigurator.ViewModels;

using System.ComponentModel;

public class ClearQuestsViewModel
{
    [DisplayName("Instance Name")]
    public string? InstanceName { get; set; }

    [DisplayName("Geofence Name")]
    public string? GeofenceName { get; set; }
}