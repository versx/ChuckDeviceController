namespace ChuckDeviceConfigurator.Controllers
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;

    using ChuckDeviceConfigurator.Data;
    using ChuckDeviceConfigurator.ViewModels;

    [Authorize(Roles = RoleConsts.UsersRole)]
    public class UserController : Controller
    {
        private readonly ILogger<UserController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UserController(
            ILogger<UserController> logger,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _logger = logger;
            _roleManager = roleManager;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.ToListAsync();
            var userRolesViewModel = new List<UserRolesViewModel>();
            foreach (var user in users)
            {
                var viewModel = new UserRolesViewModel
                {
                    UserId = user.Id,
                    Email = user.Email,
                    UserName = user.UserName,
                    Roles = await GetUserRoles(user),
                };
                userRolesViewModel.Add(viewModel);
            }
            return View(userRolesViewModel);
        }

        public async Task<IActionResult> Manage(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                ViewBag.ErrorMessage = $"User with Id '{userId}' not found";
                return View("NotFound");
            }

            ViewBag.userId = userId;
            ViewBag.UserName = user.UserName;
            var model = new List<ManageUserRolesViewModel>();
            foreach (var role in _roleManager.Roles)
            {
                var userRolesViewModel = new ManageUserRolesViewModel
                {
                    RoleId = role.Id,
                    RoleName = role.Name,
                    Selected = false,// TODO: Fix "This MySqlConnection is already in use" await _userManager.IsInRoleAsync(user, role.Name),
                };
                model.Add(userRolesViewModel);
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Manage(List<ManageUserRolesViewModel> model, string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return View();
            }
            var roles = await _userManager.GetRolesAsync(user);
            var result = await _userManager.RemoveFromRolesAsync(user, roles);
            if (!result.Succeeded)
            {
                ModelState.AddModelError("", "Cannot remove user existing roles");
                return View(model);
            }

            result = await _userManager.AddToRolesAsync(user, model.Where(x => x.Selected).Select(y => y.RoleName));
            if (!result.Succeeded)
            {
                ModelState.AddModelError("", "Cannot add selected roles to user");
                return View(model);
            }

            return RedirectToAction("Index");
        }

        public IActionResult Create()
        {
            var roles = new List<ManageUserRolesViewModel>();
            foreach (var role in _roleManager.Roles)
            {
                var userRolesViewModel = new ManageUserRolesViewModel
                {
                    RoleId = role.Id,
                    RoleName = role.Name,
                    Selected = false,
                };
                roles.Add(userRolesViewModel);
            }
            var model = new CreateUserViewModel
            {
                Roles = roles,
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserViewModel model)
        {
            try
            {
                if (_userManager.Users.FirstOrDefault(user => user.UserName == model.UserName) != null)
                {
                    ModelState.AddModelError("User", $"User account by name '{model.UserName}' already exists, please choose a different username. It is also possible to use your email address as your username.");
                    return View(model);
                }
                if (_userManager.Users.FirstOrDefault(user => user.Email == model.Email) != null)
                {
                    ModelState.AddModelError("User", $"User account with email '{model.Email}' already exists, please use a different email address.");
                    return View(model);
                }
                if (model.Password != model.ConfirmPassword)
                {
                    ModelState.AddModelError("User", $"Provided password and confirm password do not match.");
                    return View(model);
                }

                var user = new ApplicationUser
                {
                    UserName = model.UserName,
                    Email = model.Email,
                };

                var userResult = await _userManager.CreateAsync(user, model.Password);
                if (!userResult.Succeeded)
                {
                    var errors = string.Join("\n", userResult.Errors.Select(err => err.Description));
                    ModelState.AddModelError("User", errors);
                    return View(model);
                }

                async Task AssignDefaultRegisteredRole(ApplicationUser user)
                {
                    await _userManager.AddToRoleAsync(user, Roles.Registered.ToString());
                }

                // TODO: Might need to send confirmation email so user can login, since we have non-confirmed
                // accounts set unable to login unless confirmed.

                // Assign the default registered user role if no roles specified so the user can manage
                // their account at the very least until given more permissions/access by an Admin.
                if (model.Roles.Count == 0)
                {
                    await AssignDefaultRegisteredRole(user);
                }
                else
                {
                    var roleNames = model.Roles.Where(role => role.Selected).Select(role => role.RoleName);
                    var rolesResult = await _userManager.AddToRolesAsync(user, roleNames);
                    if (!rolesResult.Succeeded)
                    {
                        var errors = string.Join("\n", rolesResult.Errors.Select(err => err.Description));
                        _logger.LogError($"Failed to assign roles to user account '{model.UserName}'. Returned errors: {errors}");
                    }

                    // REVIEW: Might want to make this configurable, unsure at the moment
                    if (!await _userManager.IsInRoleAsync(user, Roles.Registered.ToString()))
                    {
                        // User not assigned default registered role, assign it
                        await AssignDefaultRegisteredRole(user);
                    }
                }

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                ModelState.AddModelError("User", $"Unknown error occurred while creating new user account.");
                return View(model);
            }
        }

        private async Task<List<string>> GetUserRoles(ApplicationUser user)
        {
            return new List<string>(await _userManager.GetRolesAsync(user));
        }
    }
}