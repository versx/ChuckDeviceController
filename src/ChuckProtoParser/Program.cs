namespace ChuckProtoParser
{
    using System;
    using System.IO;

    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Hosting;

    using Chuck.Extensions;

    public class Program
    {
        public static void Main(string[] args)
        {
            ConsoleExt.WriteInfo($"ProtoParser starting...");
            CreateHostBuilder(args).Build().Run();
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
                    webBuilder.UseUrls(config["ParserUrls"]);
                });
        }
    }
}