namespace RobotsPlugin.ViewModels;

using System.ComponentModel;

public class UserAgentRouteViewModel
{
    #region Properties

    [DisplayName("ID")]
    public Guid? Id { get; set; } = Guid.Empty;

    [DisplayName("User Agent")]
    public string UserAgent { get; set; } = null!;

    [DisplayName("Route")]
    public string Route { get; set; } = null!;

    [DisplayName("Comment")]
    public string? Comment { get; set; }

    [DisplayName("Is Allowed")]
    public bool IsAllowed { get; set; }

    [DisplayName("Is Custom")]
    public bool IsCustom { get; set; }

    #endregion

    #region Constructor

    public UserAgentRouteViewModel()
    {
    }

    public UserAgentRouteViewModel(Guid id, string userAgent, string route, string? comment = null, bool isAllowed = false, bool isCustom = false)
    {
        if (string.IsNullOrEmpty(userAgent))
            throw new ArgumentNullException(nameof(userAgent));

        if (string.IsNullOrEmpty(route))
            throw new ArgumentNullException(nameof(route));

        Id = id;
        UserAgent = userAgent;
        Route = route;
        Comment = comment ?? string.Empty;
        IsAllowed = isAllowed;
        IsCustom = isCustom;
    }

    #endregion
}