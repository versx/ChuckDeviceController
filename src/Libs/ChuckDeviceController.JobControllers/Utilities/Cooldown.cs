namespace ChuckDeviceController.JobControllers.Utilities;

using ChuckDeviceController.Data.Abstractions;
using ChuckDeviceController.Data.Entities;
using ChuckDeviceController.Data.Repositories.Dapper;
using ChuckDeviceController.Extensions;
using ChuckDeviceController.Geometry.Extensions;
using ChuckDeviceController.Geometry.Models;
using ChuckDeviceController.Geometry.Models.Abstractions;
using ChuckDeviceController.JobControllers;

public static class Cooldown
{
    public static double GetCooldownAmount(double distanceM)
    {
        return Math.Min(Convert.ToInt32(distanceM / 9.8), Strings.CooldownLimitS);
    }

    public static Coordinate? GetLastLocation(IAccount? account, string uuid)
    {
        double? lat = null;
        double? lon = null;
        if (account != null)
        {
            lat = account.LastEncounterLatitude;
            lon = account.LastEncounterLongitude;
        }
        if (lat == null || lon == null)
        {
            return null;
        }
        return new Coordinate(lat.Value, lon.Value);
    }

    public static CooldownResult SetCooldown(IAccount? account, ICoordinate location)
    {
        double? lastLat = null;
        double? lastLon = null;
        ulong? lastEncounterTime = null;
        if (account != null)
        {
            lastLat = account.LastEncounterLatitude;
            lastLon = account.LastEncounterLongitude;
            lastEncounterTime = account.LastEncounterTime;
        }

        double delay;
        ulong encounterTime;
        var now = DateTime.UtcNow.ToTotalSeconds();

        if (lastLat == null || lastLon == null || lastEncounterTime == null)
        {
            delay = 0;
            encounterTime = now;
        }
        else
        {
            var lastCoord = new Coordinate(lastLat ?? 0, lastLon ?? 0);
            var distance = lastCoord.DistanceTo(location);
            var cooldownTime = Convert.ToUInt64(lastEncounterTime + GetCooldownAmount(distance));
            encounterTime = cooldownTime < now
                ? now
                : cooldownTime;
            delay = encounterTime - now;
        }
        return new CooldownResult(delay, encounterTime);
    }

    public static async Task SetEncounterAsync(IDapperUnitOfWork uow, Account? account, ICoordinate location, ulong encounterTime)
    {
        if (uow == null)
        {
            Console.WriteLine($"Failed to set account last encounter info, provided database factory was null!");
            return;
        }

        if (account == null)
        {
            Console.WriteLine($"Failed to set account last encounter info, account was null");
            return;
        }

        var result = await uow.Accounts.UpdateAsync(account, new Dictionary<string, Func<Account, object>>
        {
            ["username"] = x => account.Username,
            ["last_encounter_lat"] = x => location.Latitude,
            ["last_encounter_lon"] = x => location.Longitude,
            ["last_encounter_time"] = x => encounterTime,
        });

        if (result == 0)
        {
            // Failed
            Console.WriteLine($"Failed to update last encounter information for account '{account.Username}'.");
        }
    }

    public static async Task SetSpinCountAsync(IDapperUnitOfWork uow, string accountUsername)
    {
        if (string.IsNullOrEmpty(accountUsername))
        {
            Console.WriteLine($"Failed to set account spin count, account username was null");
            return;
        }

        var account = await uow.Accounts.FindAsync(accountUsername);
        if (account == null)
        {
            Console.WriteLine($"Failed to increase account spin count, unable to retrieve account");
            return;
        }

        var result = await uow.Accounts.UpdateAsync(account, new Dictionary<string, Func<Account, object>>
        {
            ["username"] = x => account.Username,
            ["spins"] = x => ++account.Spins,
        });

        if (result == 0)
        {
            Console.WriteLine($"Failed to increment spin count for account '{account.Username}'.");
        }
    }
}

public class CooldownResult
{
    public double Delay { get; }

    public ulong EncounterTime { get; }

    public CooldownResult(double delay, ulong encounterTime)
    {
        Delay = delay;
        EncounterTime = encounterTime;
    }
}