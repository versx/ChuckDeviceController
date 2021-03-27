namespace DataConsumer
{
    using System;
    using System.IO;
    using System.Threading;

    using Chuck.Configuration;
    using Chuck.Extensions;

    class Program
    {
        static readonly ManualResetEvent _quitEvent = new ManualResetEvent(false);

        static void Main(string[] args)
        {
            Console.CancelKeyPress += (sender, e) =>
            {
                _quitEvent.Set();
                e.Cancel = true;
            };

            ConsoleExt.WriteInfo($"[DataConsumer] Starting...");
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

                _quitEvent.WaitOne();
                ConsoleExt.WriteInfo($"[DataConsumer] Received Ctrl+C event. Exiting...");
            }
            catch (Exception ex)
            {
                ConsoleExt.WriteError($"Config: {ex}");
                Console.ReadKey();
                return;
            }
        }
    }
}