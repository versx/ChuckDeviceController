namespace ChuckDeviceController
{
    using System;
    using System.IO;
    using System.Reflection;
    using System.Text;
    using System.Threading;

    using ChuckDeviceController.Configuration;
    using ChuckDeviceController.Data;
    using ChuckDeviceController.Extensions;

    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Hosting;

    // TODO: Add 'bootstrap_complete' property to Instance.Data for bootstrap instance, add auto_complete_instance property
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
    // TODO: Add reusable IV lists
    // TODO: Redis cache incoming requests, database consumer handles redis queue
    // TODO: Add first_seen_timestamp and last_modified_timestamp to Spawnpoints
    // TODO: Add s2cell logic route

    public static class Program
    {
        public static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            ConsoleExt.WriteDebug($"Chuck Device Controler version: v{Assembly.GetExecutingAssembly().GetName().Version}");
            ConsoleExt.WriteInfo("\tCopyright © 2021 - versx's Projects\n");
            ConsoleColor org = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("Starting ...");
            Console.ForegroundColor = org;

            var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Strings.DefaultConfigFileName);
            try
            {
                Startup.Config = Config.Load(configPath);
                if (Startup.Config == null)
                {
                    Console.WriteLine($"Failed to load config {configPath}");
                    return;
                }
            }
            catch (Exception ex)
            {
                ConsoleExt.WriteError($"Config: {ex}");
                Console.ReadKey();
                return;
            }
            // Start database migrator
            var migrator = new DatabaseMigrator();
            while (!migrator.Finished)
            {
                Thread.Sleep(50);
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
                    webBuilder.UseContentRoot(AppDomain.CurrentDomain.BaseDirectory);
                });
    }
}