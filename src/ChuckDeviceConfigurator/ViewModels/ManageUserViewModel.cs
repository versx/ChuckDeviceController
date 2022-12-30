namespace ChuckDeviceConfigurator.ViewModels;

using System.ComponentModel;

public class ManageUserViewModel
{
    [DisplayName("Username")]
    public string UserName { get; set; } = null!;

    [DisplayName("Email Address")]
    public string Email { get; set; } = null!;

    [DisplayName("Password")]
    public string Password { get; set; } = null!;

    [DisplayName("Confirm Password")]
    public string ConfirmPassword { get; set; } = null!;

    [DisplayName("Roles")]
    public List<ManageUserRolesViewModel> Roles { get; set; } = new();
}