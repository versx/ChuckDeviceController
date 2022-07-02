namespace ChuckDeviceConfigurator.Controllers
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;

    using ChuckDeviceConfigurator.Data;
    using ChuckDeviceConfigurator.ViewModels;
    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Data.Entities;

    //[FormatFilter]
    [Authorize(Roles = $"{nameof(Roles.Accounts)},{nameof(Roles.SuperAdmin)},{nameof(Roles.Admin)}")]
    public class AccountController : Controller
    {
        private readonly ILogger<AccountController> _logger;
        private readonly DeviceControllerContext _context;

        public AccountController(
            ILogger<AccountController> logger,
            DeviceControllerContext context)
        {
            _logger = logger;
            _context = context;
        }

        /*
        [HttpGet("/api/test/{id:long}.{format?}")]
        public List<Account> GetAccounts(string id)
        {
            Console.WriteLine($"Id: {id}");
            var accounts = _context.Accounts.ToList();
            return accounts;
        }
        */

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
            if (account == null)
            {
                // Failed to retrieve account from database, does it exist?
                ModelState.AddModelError("Account", $"Account does not exist with id '{id}'.");
                return View();
            }
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
        public async Task<ActionResult> Add(IFormCollection collection)
        {
            try
            {
                var level = Convert.ToUInt16(collection["Level"]);
                var accounts = Convert.ToString(collection["Accounts"]);
                accounts = accounts.Replace("<br>", "\r\n")
                                   .Replace("\r\n", "\n");
                var accountsList = accounts.Split('\n').ToList();
                if (accountsList.Count == 0)
                {
                    // No accounts provided
                    ModelState.AddModelError("Account", $"No accounts provided or parsable, please double check your format.");
                    return View();
                }

                foreach (var line in accountsList)
                {
                    var split = line.Split(',');
                    if (split.Length != 2)
                    {
                        _logger.LogWarning($"Invalid account format '{line}', skipping...");
                        continue;
                    }

                    var username = split[0];
                    var password = split[1];

                    if (_context.Accounts.Any(acc => acc.Username == username))
                    {
                        // Already exists, skip - or update? (TODO: Inform user)
                        continue;
                    }

                    var account = new Account
                    {
                        Username = username,
                        Password = password,
                        Level = level,
                    };

                    await _context.Accounts.AddAsync(account);
                    await _context.SaveChangesAsync();
                }
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                ModelState.AddModelError("Account", $"Unknown error occurred while creating new account.");
                return View();
            }
        }

        // GET: AccountController/Edit/5
        public async Task<ActionResult> Edit(string id)
        {
            var account = await _context.Accounts.FindAsync(id);
            if (account == null)
            {
                // Failed to retrieve account from database, does it exist?
                ModelState.AddModelError("Account", $"Account does not exist with id '{id}'.");
                return View();
            }
            return View(account);
        }

        // POST: AccountController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(string id, IFormCollection collection)
        {
            try
            {
                var account = await _context.Accounts.FindAsync(id);
                if (account == null)
                {
                    // Failed to retrieve account from database, does it exist?
                    ModelState.AddModelError("Account", $"Account does not exist with id '{id}'.");
                    return View();
                }
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                ModelState.AddModelError("Account", $"Unknown error occurred while editing account '{id}'.");
                return View();
            }
        }

        // GET: AccountController/Delete/5
        public async Task<ActionResult> Delete(string id)
        {
            var account = await _context.Accounts.FindAsync(id);
            if (account == null)
            {
                // Failed to retrieve account from database, does it exist?
                ModelState.AddModelError("Account", $"Account does not exist with id '{id}'.");
                return View();
            }
            return View(account);
        }

        // POST: AccountController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Delete(string id, IFormCollection collection)
        {
            try
            {
                var account = await _context.Accounts.FindAsync(id);
                if (account == null)
                {
                    // Failed to retrieve account from database, does it exist?
                    ModelState.AddModelError("Account", $"Account does not exist with id '{id}'.");
                    return View();
                }

                // Delete account from database
                _context.Accounts.Remove(account);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                ModelState.AddModelError("Account", $"Unknown error occurred while deleting account '{id}'.");
                return View();
            }
        }
    }
}