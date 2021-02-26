namespace DataConsumer
{
    using System;
    using System.IO;

    using Chuck.Infrastructure.Configuration;
    using Chuck.Infrastructure.Extensions;

    class Program
    {
        static void Main(string[] args)
        {
            var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");//Strings.DefaultConfigFileName);
            try
            {
                var config = Config.Load(configPath);
                if (config == null)
                {
                    Console.WriteLine($"Failed to load config {configPath}");
                    return;
                }

                var consumer = new DataConsumer(config);
                consumer.Start();
            }
            catch (Exception ex)
            {
                ConsoleExt.WriteError($"Config: {ex}");
                Console.ReadKey();
                return;
            }

            while (true) ;
        }
    }
}