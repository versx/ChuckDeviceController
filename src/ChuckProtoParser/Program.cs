namespace ChuckProtoParser
{
    using System;
    using System.IO;

    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Hosting;

    using Chuck.Configuration;
    using Chuck.Extensions;

    public static class Program
    {
        public static void Main(string[] args)
        {
            ConsoleExt.WriteInfo("ProtoParser starting...");
            var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");//Strings.DefaultConfigFileName);
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

            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                    webBuilder.UseUrls($"http://{Startup.Config.ParserInterface}:{Startup.Config.ParserPort}"); // TODO: Support for https and port + 1
                });
    }
}