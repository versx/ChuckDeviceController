namespace ChuckDeviceController
{
    using System;
    using System.IO;
    using System.Reflection;
    using System.Text;
    using System.Threading;

    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Hosting;

    using ChuckDeviceController.Data;

    // TODO: Add 'bootstrap_complete' property to Instance.Data for bootstrap instance, add auto_complete_instance property
    // TODO: Implement cache better
    // TODO: Use Z.EntityFramework.Cache for Cells/Pokestops/Gyms/Spawnpoints/Accounts/Devices/Instances/Assignments
    // TODO: Fix lures possible overwrite (unsure)
    // TODO: Add HasChanges property for each entity to see if needs updating
    // TODO: Add cache system for ASP.NET
    // TODO: Add reusable IV lists
    // TODO: Add s2cell logic route

    public static class Program
    {
        public static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            var org = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"Chuck Device Controller v{Assembly.GetExecutingAssembly().GetName().Version}");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("Starting...");
            Console.ForegroundColor = org;

            CreateHostBuilder(args).Build().Run();

            // Start database migrator
            var migrator = new DatabaseMigrator();
            while (!migrator.Finished)
            {
                Thread.Sleep(50);
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            var configBuilder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory());
            if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json")))
            {
                configBuilder = configBuilder.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            }
            if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), $"appsettings.{env}.json")))
            {
                configBuilder = configBuilder.AddJsonFile($"appsettings.{env}.json",
                                optional: true, reloadOnChange: true);
            }
            var config = configBuilder.AddEnvironmentVariables()
                .AddCommandLine(args)
                .Build();

            Startup.DbConnectionString = config["DbConnectionString"];

            return Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                    webBuilder.UseConfiguration(config);
                    // TODO: Support for https and port + 1
                    webBuilder.UseUrls(config["ControllerUrls"]);
                    webBuilder.UseWebRoot(Strings.WebRoot);
                });
        }
    }
}