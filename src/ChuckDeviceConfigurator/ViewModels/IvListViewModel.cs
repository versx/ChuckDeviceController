namespace ChuckDeviceConfigurator.ViewModels;

using System.ComponentModel;

public class IvListViewModel
{
    [DisplayName("Name")]
    public string Name { get; set; }

    [DisplayName("Pokemon")]
    public List<string> Pokemon { get; set; } = new();
}