namespace ChuckDeviceController
{
    using System;
    using System.IO;

    using ChuckDeviceController.Configuration;

    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Hosting;

    // TODO: Fix IV loop adding pokemon
    // TODO: Implement cache better
    // TODO: Use Z.EntityFramework.Cache for Cells/Pokestops/Gyms/Spawnpoints/Accounts/Devices/Instances/Assignments
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
    // TODO: Reusable Webhooks
    // TODO: Reusable Geofences/Circles list
    // TODO: Reusable IV lists
    // TODO: Redis cache incoming requests, database consumer handles redis queue
    // TODO: Add first_seen_timestamp and last_modified_timestamp to Spawnpoints
    // TODO: Add s2cell logic route
    // TODO: Proper error responses via Dashboard UI
    // TODO: Webhooks

    public static class Program
    {
        public static void Main(string[] args)
        {
            var configPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                Path.Combine("..", Strings.DefaultConfigFileName)
            );
            Startup.Config = Config.Load(configPath);
            if (Startup.Config == null)
            {
                Console.WriteLine($"Failed to load config {configPath}");
                return;
            }
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                    //webBuilder.UseUrls("http://localhost:5000", "https://localhost:5001");
                    webBuilder.UseUrls($"http://{Startup.Config.Interface}:{Startup.Config.Port}"); // TODO: Support for https and port + 1
                    webBuilder.UseWebRoot(Strings.WebRoot);
                });
    }
}