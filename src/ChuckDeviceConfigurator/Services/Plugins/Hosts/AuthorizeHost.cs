namespace ChuckDeviceConfigurator.Services.Plugins.Hosts;

using Microsoft.AspNetCore.Identity;

using ChuckDeviceController.Plugin;

public class AuthorizeHost : IAuthorizeHost
{
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly SemaphoreSlim _sem = new(1, 1);

    public AuthorizeHost(RoleManager<IdentityRole> roleManager)
    {
        _roleManager = roleManager;
    }

    public async Task<bool> RegisterRole(string name, int displayIndex = -1)
    {
        await _sem.WaitAsync();

        var existingRole = await _roleManager.FindByNameAsync(name);
        if (existingRole != null)
        {
            // Role with name already exists
            _sem.Release();
            return false;
        }

        var role = new IdentityRole(name)
        {
            ConcurrencyStamp = Guid.NewGuid().ToString()
        };
        var result = await _roleManager.CreateAsync(role);

        _sem.Release();
        return result.Succeeded;
    }
}