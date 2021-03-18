namespace DataConsumer
{
    using System;
    using System.IO;

    using Chuck.Configuration;
    using Chuck.Extensions;

    class Program
    {
        static void Main(string[] args)
        {
            ConsoleExt.WriteInfo($"DataConsumer starting...");
            try
            {
                var config = new Config(Directory.GetCurrentDirectory(), args);
                var consumer = new DataConsumer(config);
                consumer.Start();
            }
            catch (Exception ex)
            {
                ConsoleExt.WriteError(ex);
                Console.ReadKey();
                return;
            }

            while (true) ;
        }
    }
}