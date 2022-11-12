﻿namespace ChuckDeviceConfigurator.Data
{
    using Microsoft.AspNetCore.Identity;

    using ChuckDeviceController.Common;

    public static class UserIdentityContextSeed
    {
        public static async Task SeedSuperAdminAsync(
            UserManager<ApplicationUser> userManager)
            //RoleManager<IdentityRole> roleManager)
        {
            try
            {
                // Seed Default User
                var defaultUser = new ApplicationUser
                {
                    UserName = Strings.DefaultUserName,
                    Email = Strings.DefaultUserEmail,
                    EmailConfirmed = true,
                    PhoneNumberConfirmed = true,
                };
                if (userManager.Users.All(u => u.Id != defaultUser.Id))
                {
                    var user = await userManager.FindByNameAsync(defaultUser.UserName);
                    if (user == null)
                    {
                        await userManager.CreateAsync(defaultUser, Strings.DefaultUserPassword);
                        await userManager.AddToRolesAsync(defaultUser, new[]
                        {
                            Roles.Registered.ToString(),
                            Roles.Admin.ToString(),
                            Roles.SuperAdmin.ToString()
                        });

                        if (!await userManager.IsInRoleAsync(defaultUser, Roles.Registered.ToString()) ||
                            !await userManager.IsInRoleAsync(defaultUser, Roles.Admin.ToString()) ||
                            !await userManager.IsInRoleAsync(defaultUser, Roles.SuperAdmin.ToString()))
                        {
                            Console.WriteLine($"FAILURE: An error occurred while assigning the default user account with necessary roles!");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to create default SuperAdmin user '{Strings.DefaultUserName}'\nError: {ex}");
            }
        }

        public static async Task SeedRolesAsync(
            //UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            try
            {
                // Seed Roles
                foreach (var permission in Enum.GetNames<Roles>())
                {
                    if (!roleManager.Roles.Any(role => role.Name == permission.ToString()))
                    {
                        Console.WriteLine($"Role '{permission}' does not exist, inserting into database...");
                        await roleManager.CreateAsync(new IdentityRole(permission));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to create default roles\nError: {ex}");
            }
        }
    }
}