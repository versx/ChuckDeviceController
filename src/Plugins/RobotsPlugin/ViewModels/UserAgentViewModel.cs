namespace RobotsPlugin.ViewModels;

using System.ComponentModel;

public sealed class UserAgentViewModel
{
    #region Properties

    [DisplayName("User Agents")]
    public string UserAgent { get; } = null!;

    [DisplayName("No. Routes")]
    public uint Count { get; }

    #endregion

    #region Constructors

    public UserAgentViewModel()
    {
    }

    public UserAgentViewModel(string userAgent, uint count)
    {
        UserAgent = userAgent ?? throw new ArgumentNullException(nameof(userAgent));
        Count = count;
    }

    #endregion
}