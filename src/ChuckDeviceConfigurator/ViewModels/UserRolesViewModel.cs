namespace ChuckDeviceConfigurator.ViewModels;

public class UserRolesViewModel
{
    public string UserId { get; set; } = null!;

    public string? UserName { get; set; } = null!;

    public string? Email { get; set; }

    public IEnumerable<string>? Roles { get; set; }
}