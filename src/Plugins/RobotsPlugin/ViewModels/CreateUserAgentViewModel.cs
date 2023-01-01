namespace RobotsPlugin.ViewModels;

using System.ComponentModel;

public class CreateUserAgentViewModel
{
    [DisplayName("User Agent")]
    public string UserAgent { get; set; } = null!;
}