namespace RobotsPlugin.ViewModels;

using System.ComponentModel.DataAnnotations;

public sealed class EditRobotsViewModel
{
    #region Properties

    [Required]
    public string UserAgent { get; set; }

    [Required]
    public string Route { get; set; }

    public bool IsAllowed { get; set; }

    public List<string> UserAgents { get; private set; }

    public List<CustomUserAgentViewModel> Routes { get; private set; }

    #endregion

    #region Constructors

    public EditRobotsViewModel()
    {
    }

    public EditRobotsViewModel(List<string> userAgents, List<CustomUserAgentViewModel> routes)
    {
        UserAgents = userAgents ?? throw new ArgumentNullException(nameof(userAgents));
        Routes = routes ?? throw new ArgumentNullException(nameof(routes));
    }

    #endregion
}
