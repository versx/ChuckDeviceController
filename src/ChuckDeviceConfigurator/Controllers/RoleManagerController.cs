namespace ChuckDeviceConfigurator.Controllers
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;

    [Authorize(Roles = Strings.SuperAdminRole)]
    public class RoleManagerController : Controller
    {
        private readonly RoleManager<IdentityRole> _roleManager;

        public RoleManagerController(RoleManager<IdentityRole> roleManager)
        {
            _roleManager = roleManager;
        }

        public async Task<IActionResult> Index()
        {
            var roles = await _roleManager.Roles.ToListAsync();
            return View(roles);
        }

        [HttpPost]
        public async Task<IActionResult> AddRole(string roleName)
        {
            if (roleName != null)
            {
                await _roleManager.CreateAsync(new IdentityRole(roleName.Trim()));
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: RoleManager/Edit/5
        [HttpGet]
        public async Task<ActionResult> Edit(string id)
        {
            var role = await _roleManager.FindByIdAsync(id);
            if (role == null)
            {
                // Failed to retrieve role from database, does it exist?
                ModelState.AddModelError("Role", $"Role does not exist with id '{id}'.");
                return View();
            }
            return View(role);
        }

        // POST: RoleManager/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(string id, IFormCollection collection)
        {
            try
            {
                var role = await _roleManager.FindByIdAsync(id);
                if (role == null)
                {
                    // Failed to retrieve role from database, does it exist?
                    ModelState.AddModelError("Role", $"Role does not exist with id '{id}'.");
                    return View();
                }

                var name = Convert.ToString(collection["Name"]);
                // TODO: Check if new name already exists
                role.Name = name;

                // Update role in database
                await _roleManager.UpdateAsync(role);

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                ModelState.AddModelError("Role", $"Unknown error occurred while editing role '{id}'.");
                return View();
            }
        }

        // GET: RoleManager/Delete/5
        public async Task<ActionResult> Delete(string id)
        {
            var role = await _roleManager.FindByIdAsync(id);
            if (role == null)
            {
                // Failed to retrieve role from database, does it exist?
                ModelState.AddModelError("Role", $"Role does not exist with id '{id}'.");
                return View();
            }
            return View(role);
        }

        // POST: RoleManager/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Delete(string id, IFormCollection collection)
        {
            try
            {
                var role = await _roleManager.FindByIdAsync(id);
                if (role == null)
                {
                    // Failed to retrieve IV list from database, does it exist?
                    ModelState.AddModelError("Role", $"Role does not exist with id '{id}'.");
                    return View();
                }

                // Delete role from database
                await _roleManager.DeleteAsync(role);

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                ModelState.AddModelError("Role", $"Unknown error occurred while deleting role '{id}'.");
                return View();
            }
        }
    }
}