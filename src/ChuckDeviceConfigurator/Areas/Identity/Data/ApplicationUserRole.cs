namespace ChuckDeviceConfigurator.Areas.Identity.Data;

using Microsoft.AspNetCore.Identity;

// TODO: Use custom IdentityRole
public class UserIdentityRole : IdentityRole //Guid
{
    public int DisplayIndex { get; set; }

    public UserIdentityRole(string name, int displayIndex = -1)
        : base(name)
    {
        DisplayIndex = displayIndex;
    }
}