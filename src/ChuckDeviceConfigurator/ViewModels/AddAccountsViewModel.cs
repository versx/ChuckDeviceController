namespace ChuckDeviceConfigurator.ViewModels;

using System.ComponentModel;

public class AddAccountsViewModel
{
    [DisplayName("Level")]
    public ushort Level { get; set; }

    [DisplayName("Accounts")]
    public string? Accounts { get; set; }

    [DisplayName("Group Name")]
    public string? Group { get; set; }
}