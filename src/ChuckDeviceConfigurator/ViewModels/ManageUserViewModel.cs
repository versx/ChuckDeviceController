namespace ChuckDeviceConfigurator.ViewModels;

using System.ComponentModel;

public class ManageUserViewModel
{
    [DisplayName("Username")]
    public string? UserName { get; set; }

    [DisplayName("Email Address")]
    public string? Email { get; set; }

    [DisplayName("Password")]
    public string? Password { get; set; }

    [DisplayName("Confirm Password")]
    public string? ConfirmPassword { get; set; }

    [DisplayName("Roles")]
    public List<ManageUserRolesViewModel> Roles { get; set; } = new();
}