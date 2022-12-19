namespace ChuckDeviceConfigurator.HostedServices;

using System.Timers;

using ChuckDeviceController.Data.Entities;
using ChuckDeviceController.Data.Repositories;
using ChuckDeviceController.Extensions;

public class AccountStatusService : IHostedService, IAccountStatusService
{
    #region Constants

    private const uint SuspendedPeriodS = 2592000;
    private const uint WarningPeriodS = 604800;
    private const uint CooldownPeriodS = 7200;
    private const string FailedGprRedWarning = "GPR_RED_WARNING";
    private const string FailedSuspended = "suspended";
    private const uint IntervalS = 10; // 10 seconds

    #endregion

    #region Variables

    private readonly ILogger<AccountStatusService> _logger;
    private readonly Timer _timer;

    #endregion

    #region Properties

    public IServiceProvider Services { get; }

    #endregion

    #region Constructor

    public AccountStatusService(
        ILogger<AccountStatusService> logger,
        IServiceProvider services)
    {
        _logger = logger;
        Services = services;

        _timer = new Timer(IntervalS * 60 * 1000);
        _timer.Elapsed += async (sender, e) => await CheckAccountStatus();
    }

    #endregion

    #region Public Methods

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (!_timer.Enabled)
        {
            _timer.Start();
        }

        await Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_timer.Enabled)
        {
            _timer.Stop();
        }

        await Task.CompletedTask;
    }

    #endregion

    #region Private Methods

    private async Task CheckAccountStatus()
    {
        using var scope = Services.CreateScope();
        var serviceProvider = scope.ServiceProvider;
        using var uow = serviceProvider.GetRequiredService<IUnitOfWork>();

        var accounts = await uow.Accounts.FindAsync(x => x.Failed != null || x.FailedTimestamp != null || x.FirstWarningTimestamp != null);
        var count = accounts.Count();
        _logger.LogDebug($"Checking status of {count:N0} accounts...");

        var accountsToClear = new List<Account>();
        foreach (var account in accounts)
        {
            switch (account.Status)
            {
                case "Warning":
                    if (!IsAccountWarningExpired(account))
                        continue;

                    // Clear warning columns
                    account.Failed = null;
                    account.FailedTimestamp = null;
                    account.FirstWarningTimestamp = null;
                    account.WarnExpireTimestamp = null;
                    account.HasWarn = null;
                    accountsToClear.Add(account);
                    break;
                case "Suspended":
                    if (!IsAccountSuspensionExpired(account))
                        continue;

                    // Clear suspension columns
                    account.Failed = null;
                    account.FailedTimestamp = null;
                    account.WasSuspended = null;
                    accountsToClear.Add(account);
                    break;
                case "Cooldown":
                    if (!IsAccountCooldownExpired(account))
                        continue;

                    // Clear cooldown columns
                    account.LastEncounterLatitude = null;
                    account.LastEncounterLongitude = null;
                    account.LastEncounterTime = null;
                    accountsToClear.Add(account);
                    break;
            }
        }

        if (!accountsToClear.Any())
            return;

        _logger.LogDebug($"Clearing {accountsToClear.Count:N0} account punishment statuses...");
        await uow.Accounts.UpdateRangeAsync(accountsToClear);
        var status = await uow.CommitAsync();
        if (!status)
        {
            _logger.LogError($"Failed to update account status...");
            return;
        }

        _logger.LogInformation($"Cleared {accountsToClear.Count:N0} account punishment statuses");
    }

    private static bool IsAccountWarningExpired(Account account)
    {
        var now = DateTime.UtcNow.ToTotalSeconds();
        var status = 
            (account.Failed == FailedGprRedWarning && account.FailedTimestamp <= now - WarningPeriodS) ||
            (account.FirstWarningTimestamp > 0 && account.FirstWarningTimestamp <= now - WarningPeriodS) ||
            (account.HasWarn ?? false && account.WarnExpireTimestamp <= now - WarningPeriodS);
        return status;
    }

    private static bool IsAccountSuspensionExpired(Account account)
    {
        var now = DateTime.UtcNow.ToTotalSeconds();
        var status = 
            (account.Failed == FailedSuspended || (account.WasSuspended ?? false)) &&
            account.FailedTimestamp <= now - SuspendedPeriodS;
        return status;
    }

    public static bool IsAccountCooldownExpired(Account account)
    {
        var now = DateTime.UtcNow.ToTotalSeconds();
        var status =
            account.LastEncounterTime > 0 &&
            account.LastEncounterTime <= now - CooldownPeriodS;
        return status;
    }

    #endregion
}