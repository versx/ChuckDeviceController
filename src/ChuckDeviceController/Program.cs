namespace ChuckDeviceController
{
    using System;
    using System.IO;
    using System.Reflection;
    using System.Text;
    using System.Threading;

    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Hosting;

    using Chuck.Configuration;
    using Chuck.Extensions;
    using ChuckDeviceController.Data;

    // TODO: Add 'bootstrap_complete' property to Instance.Data for bootstrap instance, add auto_complete_instance property
    // TODO: Implement cache better
    // TODO: Add HasChanges property for each entity to see if needs updating
    // TODO: Add cache system for ASP.NET
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
                ConsoleExt.WriteError($"Config: {ex.Message}");
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
                    webBuilder.UseUrls($"http://{Startup.Config.ControllerInterface}:{Startup.Config.ControllerPort}"); // TODO: Support for https and port + 1
                    webBuilder.UseWebRoot(Strings.WebRoot);
                });
    }
}