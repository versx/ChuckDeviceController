namespace ChuckDeviceConfigurator.Utilities;

using Microsoft.EntityFrameworkCore;

using ChuckDeviceController.Data.Abstractions;
using ChuckDeviceController.Data.Contexts;
using ChuckDeviceController.Data.Entities;
using ChuckDeviceController.Extensions;
using ChuckDeviceController.Geometry.Extensions;
using ChuckDeviceController.Geometry.Models;

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

    public static CooldownResult SetCooldown(IAccount? account, Coordinate location)
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

    public static async Task SetEncounterAsync(IDbContextFactory<ControllerDbContext> factory, Account? account, Coordinate location, ulong encounterTime)
    {
        if (factory == null)
        {
            Console.WriteLine($"Failed to set account last encounter info, provided database factory was null!");
            return;
        }

        if (account == null)
        {
            Console.WriteLine($"Failed to set account last encounter info, account was null");
            return;
        }

        using (var context = factory.CreateDbContext())
        {
            context.Attach(account);
            account.LastEncounterLatitude = location.Latitude;
            account.LastEncounterLongitude = location.Longitude;
            account.LastEncounterTime = encounterTime;
            context.Entry(account).Property(p => p.LastEncounterLatitude).IsModified = true;
            context.Entry(account).Property(p => p.LastEncounterLongitude).IsModified = true;
            context.Entry(account).Property(p => p.LastEncounterTime).IsModified = true;

            await context.SaveChangesAsync();
        }
    }

    public static async Task SetSpinCountAsync(IDbContextFactory<ControllerDbContext> factory, string accountUsername)
    {
        if (string.IsNullOrEmpty(accountUsername))
        {
            Console.WriteLine($"Failed to set account spin count, account username was null");
            return;
        }

        using (var context = factory.CreateDbContext())
        {
            var account = await context.Accounts.FindAsync(accountUsername);
            if (account == null)
            {
                Console.WriteLine($"Failed to increase account spin count, unable to retrieve account");
                return;
            }

            context.Attach(account);
            account.Spins++;
            context.Entry(account).Property(p => p.Spins).IsModified = true;
            await context.SaveChangesAsync();
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