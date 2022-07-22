namespace ChuckDeviceConfigurator.Controllers
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;

    using ChuckDeviceConfigurator.ViewModels;
    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Data.Entities;

    //[FormatFilter]
    [Authorize(Roles = RoleConsts.AccountsRole)]
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
        public ActionResult Index(int page = 1, int pageSize = 100)
        {
            //var accounts = _context.Accounts.ToList();
            var accounts = _context.Accounts.Skip((page - 1) * pageSize)
                                            .Take(pageSize)
                                            .ToList();
            var accountsInUse = _context.Devices.Where(device => device.AccountUsername != null)
                                                .Select(device => device.AccountUsername)
                                                .ToList();
            var count = _context.Accounts.Count(); //accounts.Count;
            /*
            var data = accounts.Skip((page - 1) * pageSize)
                               .Take(pageSize)
                               .ToList();
            */
            accounts?.ForEach(account => account.IsInUse = accountsInUse.Contains(account.Username));

            ViewBag.MaxPage = ((count / pageSize) - (count % pageSize == 0 ? 1 : 0)) + 1;
            ViewBag.Page = page;
            return View(new ViewModelsModel<Account>
            {
                Items = accounts ?? new(),
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
                Group = null,
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
                var group = Convert.ToString(collection["Group"]);
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
                        GroupName = string.IsNullOrEmpty(group)
                            ? null
                            : group,
                    };

                    await _context.Accounts.AddAsync(account);
                    await _context.SaveChangesAsync();
                }
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                ModelState.AddModelError("Account", $"Unknown error occurred while importing new accounts.");
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

                var username = Convert.ToString(collection["Username"]);
                var password = Convert.ToString(collection["Password"]);
                var level = Convert.ToUInt16(collection["Level"]);
                var spins = Convert.ToUInt32(collection["Spins"]);
                var tutorial = Convert.ToUInt16(collection["Tutorial"]);
                var group = Convert.ToString(collection["GroupName"]);
                account.Username = username;
                account.Password = password;
                account.Level = level;
                account.Spins = spins;
                account.Tutorial = tutorial;
                account.GroupName = string.IsNullOrEmpty(group)
                    ? null
                    : group;
                _context.Update(account);
                await _context.SaveChangesAsync();

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