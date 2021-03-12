namespace DataConsumer
{
    using System;
    using System.IO;

    using Chuck.Configuration;
    using Chuck.Extensions;

    internal static class Program
    {
        private static void Main(/*string[] args*/)
        {
            ConsoleExt.WriteInfo("DataConsumer starting...");
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