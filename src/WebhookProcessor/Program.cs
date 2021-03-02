namespace WebhookProcessor
{
    using System;
    using System.IO;

    using StackExchange.Redis;

    using Chuck.Infrastructure.Configuration;
    using Chuck.Infrastructure.Extensions;

    class Program
    {
        static IConnectionMultiplexer _redis;
        static ISubscriber _subscriber;
        static Config _config;

        static void Main(string[] args)
        {
            var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");//Strings.DefaultConfigFileName);
            try
            {
                _config = Config.Load(configPath);
                if (_config == null)
                {
                    Console.WriteLine($"Failed to load config {configPath}");
                    return;
                }

                var options = new ConfigurationOptions
                {
                    EndPoints =
                    {
                        { $"{_config.Redis.Host}:{_config.Redis.Port}" }
                    },
                    Password = _config.Redis.Password,
                };
                _redis = ConnectionMultiplexer.Connect(options);
                _redis.ConnectionFailed += RedisOnConnectionFailed;
                _redis.ErrorMessage += RedisOnErrorMessage;
                _redis.InternalError += RedisOnInternalError;
                if (_redis.IsConnected)
                {
                    _subscriber = _redis.GetSubscriber();
                    _subscriber.Subscribe(_config.Redis.QueueName, SubscriptionHandler);
                    //_redisDatabase = _redis.GetDatabase(_config.Redis.DatabaseNum);
                }

                WebhookController.Instance.SleepIntervalS = 5;
                WebhookController.Instance.Webhooks = _config.Webhooks;
                WebhookController.Instance.Start();
            }
            catch (Exception ex)
            {
                ConsoleExt.WriteError($"[DataConsumer] Error: {ex}");
                Console.ReadKey();
                return;
            }

            while (true) ;
        }

        #region Redis Events

        static void RedisOnConnectionFailed(object sender, ConnectionFailedEventArgs e)
        {
            ConsoleExt.WriteError($"[DataConsumer] [Redis] {e.EndPoint}: {e.FailureType} {e.Exception}");
        }

        static void RedisOnErrorMessage(object sender, RedisErrorEventArgs e)
        {
            ConsoleExt.WriteError($"[DataConsumer] [Redis] {e.EndPoint}: {e.Message}");
        }

        static void RedisOnInternalError(object sender, InternalErrorEventArgs e)
        {
            ConsoleExt.WriteError($"[DataConsumer] [Redis] {e.EndPoint}: {e.Exception}");
        }

        #endregion

        static void SubscriptionHandler(RedisChannel channel, RedisValue message)
        {
            if (string.IsNullOrEmpty(message)) return;

            var payload = (dynamic)message;
            if (payload == null) return;

            WebhookController.Instance.Add(payload);
        }
    }
}