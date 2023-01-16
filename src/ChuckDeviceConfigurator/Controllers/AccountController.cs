namespace ChuckDeviceConfigurator.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using ChuckDeviceConfigurator.ViewModels;
using ChuckDeviceController.Common;
using ChuckDeviceController.Data.Entities;
using ChuckDeviceController.Data.Repositories;
using ChuckDeviceController.Extensions;
using ChuckDeviceController.Plugin.Helpers;

//[FormatFilter]
[Authorize(Roles = RoleConsts.AccountsRole)]
public class AccountController : BaseMvcController
{
    private readonly ILogger<AccountController> _logger;
    private readonly IUnitOfWork _uow;

    public AccountController(
        ILogger<AccountController> logger,
        IUnitOfWork uow)
    {
        _logger = logger;
        _uow = uow;
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
    public async Task<ActionResult> Index(string? accountGroup = null)
    {
        var allAccounts = await _uow.Accounts.FindAllAsync();
        var accounts = FilterByGroup(allAccounts, accountGroup);
        var devices = await _uow.Devices.FindAsync(x => x.AccountUsername != null);
        var deviceAccounts = devices.Select(x => x.AccountUsername).ToList();
        var accountsInUse = devices
            .Where(device => device.AccountUsername != null)
            .Select(device => device.AccountUsername)
            .ToList();
        var accountGroups = allAccounts
            .Select(x => x.GroupName)
            .Distinct()
            .Where(x => !string.IsNullOrEmpty(x))
            .OrderBy(x => x)
            .ToList();

        var total = accounts.Count();
        var cleanAccounts = accounts.Where(x => x.IsAccountClean).ToList();
        var accountLevelStatistics = accounts.GroupBy(account => account.Level, (level, levelAccounts) => new AccountLevelStatisticsViewModel
        {
            Level = level,
            Total = (ulong)levelAccounts.LongCount(),
            InUse = (ulong)levelAccounts.LongCount(acc => deviceAccounts.Contains(acc.Username) && acc.Level == level),
            Good = (ulong)levelAccounts.LongCount(acc => acc.IsAccountClean),
            Banned = (ulong)levelAccounts.LongCount(acc => acc.IsAccountBanned),
            Warning = (ulong)levelAccounts.LongCount(acc => acc.IsAccountWarned),
            Suspended = (ulong)levelAccounts.LongCount(acc => acc.IsAccountSuspended),
            Invalid = (ulong)levelAccounts.LongCount(acc => acc.IsAccountInvalidCredentials),
            Cooldown = (ulong)levelAccounts.LongCount(acc => acc.IsAccountInCooldown),
            SpinLimit = (ulong)levelAccounts.LongCount(acc => acc.Spins >= Strings.DefaultSpinLimit),
            // error_26, etc
            //Other = (ulong)levelAccounts.LongCount(acc => !string.IsNullOrEmpty(acc.Failed) && !Account.FailedReasons.Contains(acc.Failed)),
        }).ToList();

        var days7 = Strings.OneDayS * 7;
        var days30 = Strings.OneDayS * 30;
        var now = DateTime.UtcNow.ToTotalSeconds();
        var bannedAccounts = accounts.Where(x => x.IsAccountBanned);
        var warnedAccounts = accounts.Where(x => x.IsAccountWarned);
        var suspendedAccounts = accounts.Where(x => x.IsAccountSuspended);

        var model = new AccountStatisticsViewModel
        {
            //Accounts = accounts ?? new(),
            AccountLevelStatistics = accountLevelStatistics,
            TotalAccounts = (ulong)total,
            InCooldown = (ulong)accounts!.LongCount(x => x.IsAccountInCooldown),
            AccountsInUse = (ulong)accountsInUse.Count,
            OverSpinLimit = (ulong)accounts!.LongCount(x => x.Spins >= Strings.DefaultSpinLimit),
            CleanLevel40s = (ulong)cleanAccounts.LongCount(x => x.IsLevel40OrHigher),
            CleanLevel30s = (ulong)cleanAccounts.LongCount(x => x.IsLevel30OrHigher),
            SuspendedAccounts = (ulong)suspendedAccounts.LongCount(),
            CleanAccounts = (ulong)cleanAccounts.Count,
            FreshAccounts = (ulong)cleanAccounts.LongCount(x => x.IsNewAccount),
            Level40OrHigher = (ulong)accounts!.LongCount(x => x.IsLevel40OrHigher),
            Level30OrHigher = (ulong)accounts!.LongCount(x => x.IsLevel30OrHigher),
            Bans = new AccountPunishmentsViewModel
            {
                Last24Hours = (ulong)bannedAccounts.LongCount(x => now - x.FailedTimestamp < Strings.OneDayS),
                Last7Days = (ulong)bannedAccounts.LongCount(x => now - x.FailedTimestamp < days7),
                Last30Days = (ulong)bannedAccounts.LongCount(x => now - x.FailedTimestamp < days30),
                Total = (ulong)bannedAccounts.LongCount(),
            },
            Warnings = new AccountPunishmentsViewModel
            {
                Last24Hours = (ulong)warnedAccounts.LongCount(x => now - x.FirstWarningTimestamp < Strings.OneDayS || now - x.WarnExpireTimestamp < Strings.OneDayS),
                Last7Days = (ulong)warnedAccounts.LongCount(x => now - x.FirstWarningTimestamp < days7 || now - x.WarnExpireTimestamp < days7),
                Last30Days = (ulong)warnedAccounts.LongCount(x => now - x.FirstWarningTimestamp < days30 || now - x.WarnExpireTimestamp < days30),
                Total = (ulong)warnedAccounts.LongCount(),
            },
            Suspensions = new AccountPunishmentsViewModel
            {
                Last24Hours = (ulong)suspendedAccounts.LongCount(x => now - x.FailedTimestamp < Strings.OneDayS || now - x.FailedTimestamp < Strings.OneDayS),
                Last7Days = (ulong)suspendedAccounts.LongCount(x => now - x.FailedTimestamp < days7 || now - x.FailedTimestamp < days7),
                Last30Days = (ulong)suspendedAccounts.LongCount(x => now - x.FailedTimestamp < days30 || now - x.FailedTimestamp < days30),
                Total = (ulong)suspendedAccounts.LongCount(),
            },
        };

        ViewBag.AccountGroups = accountGroups;
        ViewBag.SelectedGroup = accountGroup;
        return View(model);
    }

    [HttpPost]
    [Produces("application/json")]
    public async Task<ActionResult> GetAccounts(string? accountGroup = null, bool? formatted = false)
    {
        var allAccounts = await _uow.Accounts.FindAllAsync();
        var accounts = FilterByGroup(allAccounts, accountGroup);

        if (!(formatted ?? false))
        {
            return new JsonResult(new { accounts });
        }

        var formattedAccounts = new List<dynamic>();
        foreach (var account in accounts)
        {
            formattedAccounts.Add(new
            {
                account.Username,
                account.Password,
                account.Level,
                account.Spins,
                account.Tutorial,
                status = new
                {
                    formatted = Utils.GetAccountStatusColor(account.Status),
                    sort = account.Status,
                },
                group = account.GroupName,
                last_encounter = new
                {
                    formatted = Utils.GetLastUpdatedStatus(account.LastEncounterTime ?? 0),
                    sort = account.LastEncounterTime,
                },
                in_use = Utils.FormatBoolean(account.IsInUse),
                action = "",
            });
        }

        return new JsonResult(new { accounts = formattedAccounts });
    }

    // GET: AccountController/Details/5
    public async Task<ActionResult> Details(string id)
    {
        var account = await _uow.Accounts.FindByIdAsync(id);
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
                CreateErrorNotification($"Failed to parse accounts to import!");
                return View();
            }

            var accountsToAdd = new List<Account>();
            foreach (var line in accountsList)
            {
                // Support `, : ;` delimiters
                var split = line.Split(new[] { ',', ';', ':' }, StringSplitOptions.RemoveEmptyEntries);
                if (split.Length != 2)
                {
                    _logger.LogWarning($"Invalid account format '{line}', skipping...");
                    continue;
                }

                var username = split[0];
                var password = split[1];

                if (_uow.Accounts.Any(acc => acc.Username == username))
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
                accountsToAdd.Add(account);
            }

            await _uow.Accounts.AddRangeAsync(accountsToAdd);
            await _uow.CommitAsync();

            CreateSuccessNotification($"Imported '{accountsToAdd:N0}' level {level} accounts successfully!");

            return RedirectToAction(nameof(Index));
        }
        catch
        {
            ModelState.AddModelError("Account", $"Unknown error occurred while importing new accounts.");
            CreateErrorNotification($"Unknown error occurred while importing new accounts!");
            return View();
        }
    }

    // GET: AccountController/Edit/5
    public async Task<ActionResult> Edit(string id)
    {
        var account = await _uow.Accounts.FindByIdAsync(id);
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
            var account = await _uow.Accounts.FindByIdAsync(id);
            if (account == null)
            {
                // Failed to retrieve account from database, does it exist?
                ModelState.AddModelError("Account", $"Account does not exist with id '{id}'.");
                CreateErrorNotification($"Account does not exist with id '{id}'!");
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
            await _uow.Accounts.UpdateAsync(account);
            await _uow.CommitAsync();

            CreateSuccessNotification($"Updated account '{account.Username}' successfully!");

            return RedirectToAction(nameof(Index));
        }
        catch
        {
            ModelState.AddModelError("Account", $"Unknown error occurred while editing account '{id}'.");
            CreateErrorNotification($"Unknown error occurred while editing account '{id}'!");
            return View();
        }
    }

    // GET: AccountController/Delete/5
    public async Task<ActionResult> Delete(string id)
    {
        var account = await _uow.Accounts.FindByIdAsync(id);
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
            var account = await _uow.Accounts.FindByIdAsync(id);
            if (account == null)
            {
                // Failed to retrieve account from database, does it exist?
                ModelState.AddModelError("Account", $"Account does not exist with id '{id}'.");
                CreateErrorNotification($"Account does not exist with id '{id}'!");
                return View();
            }

            // Delete account from database
            await _uow.Accounts.RemoveAsync(account);
            await _uow.CommitAsync();

            CreateSuccessNotification($"Successfully deleted account account '{id}'!");

            return RedirectToAction(nameof(Index));
        }
        catch
        {
            ModelState.AddModelError("Account", $"Unknown error occurred while deleting account '{id}'.");
            CreateErrorNotification($"Unknown error occurred while editing account '{id}'!");
            return View();
        }
    }

    private static IEnumerable<Account> FilterByGroup(IEnumerable<Account> allAccounts, string? accountGroup = null)
    {
        var onlyGroups = accountGroup == "all_groups";
        var onlyNoGroups = accountGroup == "no_groups";
        var accounts = allAccounts
            .Where(x =>
                // All accounts with and without groups
                (!onlyGroups && !onlyNoGroups && string.IsNullOrEmpty(accountGroup)) ||
                // Only accounts with a group
                (onlyGroups && x.GroupName != null) ||
                // Only accounts without a group
                (onlyNoGroups && x.GroupName == null) ||
                // All matching accounts with a group
                (!string.IsNullOrEmpty(x.GroupName) && accountGroup == x.GroupName)
            )
            .ToList();
        return accounts;
    }
}