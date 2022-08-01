namespace ChuckDeviceConfigurator.Controllers
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;

    using ChuckDeviceConfigurator.Utilities;
    using ChuckDeviceConfigurator.ViewModels;
    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Extensions;

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
            var accounts = _context.Accounts.ToList();
            var devices = _context.Devices.ToList();

            var total = accounts.Count;
            var maxPage = (total / pageSize) - (total % pageSize == 0 ? 1 : 0) + 1;
            page = page > maxPage ? maxPage : page;

            var pagedAccounts = accounts.OrderBy(key => key.Username)
                                        .Skip((page - 1) * pageSize)
                                        .Take(pageSize)
                                        .ToList();
            var accountsInUse = devices.Where(device => device.AccountUsername != null)
                                       .Select(device => device.AccountUsername)
                                       .ToList();

            var now = DateTime.UtcNow.ToTotalSeconds();

            var banExpireTime = Strings.OneDayS * 7;
            var failedList = new List<string> { "banned", "suspended", "invalid_credentials", "GPR_RED_WARNING", "GPR_BANNED" };
            var cleanAccounts = accounts.Where(x => string.IsNullOrEmpty(x.Failed) &&
                                                    x.FailedTimestamp == null &&
                                                    x.FirstWarningTimestamp == null &&
                                                    x.LastEncounterLatitude == null &&
                                                    x.LastEncounterLongitude == null &&
                                                    x.LastEncounterTime == null &&
                                                    x.Spins == 0);
            var accountLevelStatistics = accounts.GroupBy(x => x.Level, (x, y) => new AccountLevelStatisticsViewModel
            {
                Level = x,
                Total = (ulong)accounts.LongCount(z => z.Level == x),
                InUse = (ulong)accounts.LongCount(z => devices.Any(dev => string.Compare(dev.AccountUsername, z.Username) == 0 && z.Level == x)),
                Good = (ulong)y.LongCount(z => string.IsNullOrEmpty(z.Failed) && z.FailedTimestamp == null && z.FirstWarningTimestamp == null),
                Banned = (ulong)y.LongCount(z => (z.Failed == "banned" || z.Failed == "GPR_BANNED") && now - z.FailedTimestamp < banExpireTime),
                Warning = (ulong)y.LongCount(z => z.FirstWarningTimestamp > 0),
                Suspended = (ulong)y.LongCount(z => z.Failed == "suspended"),
                Invalid = (ulong)y.LongCount(z => z.Failed == "invalid_credentials"),
                Cooldown = (ulong)y.LongCount(z => z.LastEncounterTime != null && now - z.LastEncounterTime < 7200),
                SpinLimit = (ulong)y.LongCount(z => z.Spins >= Strings.DefaultSpinLimit),
                Other = (ulong)y.LongCount(z => !failedList.Contains(z.Failed)),
            }).OrderByDescending(x => x.Level).ToList();
            //accountLevelStatistics.Sort((a, b) => b.Level.CompareTo(a.Level));

            /*
            if (FirstWarningTimestamp > 0)
                return "Warning";
            if (WasSuspended ?? false)
                return "Warning";
             */
            var days7 = Strings.OneDayS * 7;
            var days30 = Strings.OneDayS * 30;
            var bannedAccounts = accounts.Where(x => x.Failed == "banned" || x.Failed == "GPR_BANNED" || (x.Banned ?? false));
            var warnedAccounts = accounts.Where(x => x.Failed == "GPR_RED_WARNING" || (x.Warn ?? false) || x.FirstWarningTimestamp > 0);
            var suspendedAccounts = accounts.Where(x => x.Failed == "suspended" || (x.WasSuspended ?? false));

            var model = new AccountStatisticsViewModel
            {
                Accounts = pagedAccounts ?? new(),
                AccountLevelStatistics = accountLevelStatistics,
                TotalAccounts = (ulong)total,
                InCooldown = (ulong)accounts.LongCount(x => x.LastEncounterTime > 0 && now - x.LastEncounterTime < Strings.CooldownLimitS),
                AccountsInUse = (ulong)accountsInUse.Count,
                OverSpinLimit = (ulong)accounts.LongCount(x => x.Spins >= Strings.DefaultSpinLimit),
                CleanLevel30s = (ulong)cleanAccounts.LongCount(x => x.Level >= 30),
                SuspendedAccounts = (ulong)accounts.LongCount(x =>  x.Failed == "suspended"),
                NewAccounts = (ulong)cleanAccounts.LongCount(),
                OverLevel30 = (ulong)accounts.LongCount(x => x.Level >= 30),
                Bans = new AccountWarningsBansViewModel
                {
                    Last24Hours = (ulong)bannedAccounts.LongCount(x => now - x.FailedTimestamp < Strings.OneDayS),
                    Last7Days = (ulong)bannedAccounts.LongCount(x => now - x.FailedTimestamp < days7),
                    Last30Days = (ulong)bannedAccounts.LongCount(x => now - x.FailedTimestamp < days30),
                    Total = (ulong)bannedAccounts.LongCount(),
                },
                Warnings = new AccountWarningsBansViewModel
                {
                    Last24Hours = (ulong)warnedAccounts.LongCount(x => now - x.FirstWarningTimestamp < Strings.OneDayS || now - x.WarnExpireTimestamp < Strings.OneDayS),
                    Last7Days = (ulong)warnedAccounts.LongCount(x => now - x.FirstWarningTimestamp < days7 || now - x.WarnExpireTimestamp < days7),
                    Last30Days = (ulong)warnedAccounts.LongCount(x => now - x.FirstWarningTimestamp < days30 || now - x.WarnExpireTimestamp < days30),
                    Total = (ulong)warnedAccounts.LongCount(),
                },
                Suspended = new AccountWarningsBansViewModel
                {
                },
            };
            ViewBag.MaxPage = maxPage;
            ViewBag.Page = page;
            ViewBag.NextPages = Utils.GetNextPages(page, maxPage);
            return View(model);
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