namespace RobotsPlugin.ViewModels;

using Data.Abstractions;

public class UserAgentRoutesViewModel
{
    public string UserAgent { get; set; } = null!;

    public List<IRobotRouteData> Routes { get; set; } = new();
}