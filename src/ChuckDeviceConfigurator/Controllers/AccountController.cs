namespace ChuckDeviceConfigurator.Controllers
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;

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
        public async Task<ActionResult> Index()
        {
            // NOTE: Speed up query
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            var accounts = await _context.Accounts.ToListAsync();
            var devices = await _context.Devices.ToListAsync();

            var accountsInUse = devices.Where(device => device.AccountUsername != null)
                                       .Select(device => device.AccountUsername)
                                       .ToList();

            var total = accounts.Count;
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
            var accountLevelStatistics = accounts.GroupBy(account => account.Level, (level, levelAccounts) => new AccountLevelStatisticsViewModel
            {
                Level = level,
                Total = (ulong)accounts.LongCount(acc => acc.Level == level),
                InUse = (ulong)accounts.LongCount(acc => devices.Any(dev => string.Compare(dev.AccountUsername, acc.Username) == 0 && acc.Level == level)),
                Good = (ulong)levelAccounts.LongCount(acc => string.IsNullOrEmpty(acc.Failed) && acc.FailedTimestamp == null && acc.FirstWarningTimestamp == null),
                Banned = (ulong)levelAccounts.LongCount(acc => (acc.Failed == "banned" || acc.Failed == "GPR_BANNED") && now - acc.FailedTimestamp < banExpireTime),
                Warning = (ulong)levelAccounts.LongCount(acc => acc.FirstWarningTimestamp > 0),
                Suspended = (ulong)levelAccounts.LongCount(acc => acc.Failed == "suspended"),
                Invalid = (ulong)levelAccounts.LongCount(acc => acc.Failed == "invalid_credentials"),
                Cooldown = (ulong)levelAccounts.LongCount(acc => acc.LastEncounterTime != null && now - acc.LastEncounterTime < 7200),
                SpinLimit = (ulong)levelAccounts.LongCount(acc => acc.Spins >= Strings.DefaultSpinLimit),
                Other = (ulong)levelAccounts.LongCount(acc => !string.IsNullOrEmpty(acc.Failed) && !failedList.Contains(acc.Failed)),
            }).ToList();

            var days7 = Strings.OneDayS * 7;
            var days30 = Strings.OneDayS * 30;
            var bannedAccounts = accounts.Where(x => x.Failed == "banned" || x.Failed == "GPR_BANNED" || (x.Banned ?? false));
            var warnedAccounts = accounts.Where(x => x.Failed == "GPR_RED_WARNING" || (x.Warn ?? false) || x.FirstWarningTimestamp > 0);
            var suspendedAccounts = accounts.Where(x => x.Failed == "suspended" || (x.WasSuspended ?? false));

            var model = new AccountStatisticsViewModel
            {
                Accounts = accounts ?? new(),
                AccountLevelStatistics = accountLevelStatistics,
                TotalAccounts = (ulong)total,
                InCooldown = (ulong)accounts!.LongCount(x => x.LastEncounterTime > 0 && now - x.LastEncounterTime < Strings.CooldownLimitS),
                AccountsInUse = (ulong)accountsInUse.Count,
                OverSpinLimit = (ulong)accounts!.LongCount(x => x.Spins >= Strings.DefaultSpinLimit),
                CleanLevel30s = (ulong)cleanAccounts.LongCount(x => x.Level >= 30),
                SuspendedAccounts = (ulong)suspendedAccounts.LongCount(),
                NewAccounts = (ulong)cleanAccounts.LongCount(),
                OverLevel30 = (ulong)accounts!.LongCount(x => x.Level >= 30),
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
                    Total = (ulong)suspendedAccounts.LongCount(),
                },
            };

            sw.Stop();
            var totalSeconds = Math.Round(sw.Elapsed.TotalSeconds, 4);
            _logger.LogDebug($"Account stats took {totalSeconds}s");
            // Time: 0.1302 - So it's not the query, it's Razor being slow in the frontend :(

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