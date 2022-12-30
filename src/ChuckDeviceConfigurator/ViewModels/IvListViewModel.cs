namespace ChuckDeviceConfigurator.ViewModels;

using System.ComponentModel;

public class IvListViewModel
{
    [DisplayName("Name")]
    public string Name { get; set; } = null!;

    [DisplayName("Pokemon")]
    public List<string> Pokemon { get; set; } = new();
}