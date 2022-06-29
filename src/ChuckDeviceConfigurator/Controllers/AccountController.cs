namespace ChuckDeviceConfigurator.Controllers
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;

    using ChuckDeviceConfigurator.Models;
    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Data.Entities;

    public class AccountController : Controller
    {
        private readonly DeviceControllerContext _context;

        public AccountController(DeviceControllerContext context)
        {
            _context = context;
        }

        // GET: AccountController
        public ActionResult Index()
        {
            var accounts = _context.Accounts.ToList();
            return View(new ViewModelsModel<Account>
            {
                Items = accounts,
            });
        }

        // GET: AccountController/Details/5
        public async Task<ActionResult> Details(string id)
        {
            var account = await _context.Accounts.FindAsync(id);
            return View(account);
        }

        // GET: AccountController/Add
        public ActionResult Add()
        {
            var model = new AddAccountsViewModel
            {
                Level = 30,
                Accounts = string.Empty,
            };
            return View(model);
        }

        // POST: AccountController/Add
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Add(IFormCollection collection)
        {
            try
            {
                var level = Convert.ToUInt16(collection["Level"]);
                var accounts = Convert.ToString(collection["Accounts"]);
                accounts = accounts.Replace("<br>", "\r\n")
                                   .Replace("\r\n", "\n");
                var split = accounts.Split('\n').ToList();
                foreach (var account in split)
                {
                    var line = account.Split(',');
                    if (line.Length != 2)
                    {
                        Console.WriteLine($"Invalid account format");
                        continue;
                    }
                    var username = line[0];
                    var password = line[1];
                    Console.WriteLine($"Username: {username}, Password: {password}");
                    // TODO: Add accounts to database
                }
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: AccountController/Edit/5
        public async Task<ActionResult> Edit(string id)
        {
            var account = await _context.Accounts.FindAsync(id);
            return View(account);
        }

        // POST: AccountController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(string id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: AccountController/Delete/5
        public async Task<ActionResult> Delete(string id)
        {
            var account = await _context.Accounts.FindAsync(id);
            return View(account);
        }

        // POST: AccountController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(string id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }
    }
}
