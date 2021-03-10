namespace ChuckDeviceController.Services
{
    using System;
    using System.Threading.Tasks;

    using Microsoft.AspNetCore.Identity;

    using Chuck.Data.Entities;

    public class LoginService
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;

        public LoginService(UserManager<User> userManager, SignInManager<User> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public async Task<bool> Login(string username, string password)
        {
            var user = await _userManager.FindByNameAsync(username).ConfigureAwait(false);
            if (user != null && !user.Enabled)
            {
                // User account is disabled
                return false;
            }
            if (!await _userManager.CheckPasswordAsync(user, password))
            {
                // Invalid password
                return false;
            }

            var result = await _signInManager.PasswordSignInAsync(username, password, true, true);
            return result.Succeeded;
        }

        public async Task Logout()
        {
            await _signInManager.SignOutAsync();
        }
    }
}