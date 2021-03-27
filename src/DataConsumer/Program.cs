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
            ConsoleExt.WriteInfo($"[DataConsumer] Starting...");
            Console.CancelKeyPress += (sender, e) =>
            {
                _quitEvent.Set();
                e.Cancel = true;
            };

            var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");//Strings.DefaultConfigFileName);
            try
            {
                var config = new Config(Directory.GetCurrentDirectory(), args);
                var consumer = new DataConsumer(config);
                consumer.Start();

                _quitEvent.WaitOne();
                ConsoleExt.WriteInfo($"[DataConsumer] Received Ctrl+C event. Exiting...");
            }
            catch (Exception ex)
            {
                ConsoleExt.WriteError(ex);
                Console.ReadKey();
                return;
            }
        }
    }
}