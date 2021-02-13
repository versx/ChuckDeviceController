namespace ChuckDeviceController
{
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Hosting;

    // TODO: Convert Dashboard UI times to specified timezone
    // TODO: Fix IV loop adding pokemon
    // TODO: Implement cache better
    // TODO: Use Z.EntityFramework.Cache for Cells/Pokestops/Gyms/Spawnpoints/Accounts/Devices/Instances/Assignments
    // TODO: Fix pokemon expire timestamp
    // TODO: ~~Fix pokemon IV overwrite~~
    // TODO: Fix lures possible overwrite (unsure)
    // TODO: Smart raid instance
    // TODO: Configurable Interface/Port
    // TODO: Configurable circle pokemon route type (leapfrog/spread/circular)
    // TODO: Maybe leveling instance
    // TODO: Dockerize
    // TODO: Fix issue with bootstrapping, possibly invalid polygons
    // TODO: HasChanges property for each entity to see if needs updating
    // TODO: Cache system for ASP.NET
    // TODO: Secondary cache system with EntityFrameworkCore.Plus/Extensions
    // TODO: Reuable Webhooks
    // TODO: Reuable Geofences/Circles list
    // TODO: Reusable IV lists
    // TODO: Redis cache incoming requests, database consumer handles redis queue
    // TODO: Add first_seen_timestamp and last_modified_timestamp to Spawnpoints
    // TODO: Add s2cell logic route
    // TODO: Proper error responses via Dashboard UI

    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                    webBuilder.UseWebRoot("../wwwroot");
                });
    }
}