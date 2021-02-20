namespace ChuckDeviceController
{
    using ChuckDeviceController.Configuration;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Hosting;
    using System;
    using System.IO;

    // TODO: Add auto bootstrap mode, add 'bootstrap_complete' property to Instance.Data
    // TODO: Fix IV loop adding pokemon
    // TODO: Implement cache better
    // TODO: Use Z.EntityFramework.Cache for Cells/Pokestops/Gyms/Spawnpoints/Accounts/Devices/Instances/Assignments
    // TODO: Fix lures possible overwrite (unsure)
    // TODO: Add Smart raid instance
    // TODO: Maybe leveling instance
    // TODO: Fix issue with bootstrapping, possibly invalid polygons
    // TODO: Add HasChanges property for each entity to see if needs updating
    // TODO: Add cache system for ASP.NET
    // TODO: Add secondary cache system with EntityFrameworkCore.Plus/Extensions
    // TODO: Add reusable Webhooks
    // TODO: Add reusable IV lists
    // TODO: Redis cache incoming requests, database consumer handles redis queue
    // TODO: Add first_seen_timestamp and last_modified_timestamp to Spawnpoints
    // TODO: Add s2cell logic route
    // TODO: Add proper error responses via Dashboard UI

    public static class Program
    {
        public static void Main(string[] args)
        {
            string configPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                Path.Combine("..", Strings.DefaultConfigFileName)
            );
            Startup.Config = Config.Load(configPath);
            if (Startup.Config == null)
            {
                Console.WriteLine($"Failed to load config {configPath}");
                return;
            }
            // Start database migrator
            Data.DatabaseMigrator migrator = new Data.DatabaseMigrator();
            while (!migrator.Finished)
            {
                System.Threading.Thread.Sleep(50);
            }

            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
.ConfigureWebHostDefaults(webBuilder =>
{
    webBuilder.UseStartup<Startup>();
    //webBuilder.UseUrls("http://localhost:5000", "https://localhost:5001");
    webBuilder.UseUrls($"http://{Startup.Config.Interface}:{Startup.Config.Port}"); // TODO: Support for https and port + 1
    webBuilder.UseWebRoot(Strings.WebRoot);
});
        }
    }
}