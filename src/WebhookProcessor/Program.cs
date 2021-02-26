namespace WebhookProcessor
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.Json;

    using StackExchange.Redis;

    using Chuck.Infrastructure.Common;
    using Chuck.Infrastructure.Configuration;
    using Chuck.Infrastructure.Data.Entities;
    using Chuck.Infrastructure.Extensions;

    class Program
    {
        private const string RedisQueueName = "*"; // TODO: Eventually change from wildcard

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
                    _subscriber.Subscribe(RedisQueueName, SubscriptionHandler);
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

        // TODO: Send object as json string and just add to webhook queue
        static void SubscriptionHandler(RedisChannel channel, RedisValue message)
        {
            if (string.IsNullOrEmpty(message)) return;

            switch (channel)
            {
                case RedisChannels.WebhookPokemon:
                    var pokemon = JsonSerializer.Deserialize<Pokemon>(message);
                    if (pokemon == null) return;
                    WebhookController.Instance.AddPokemon(pokemon);
                    break;
                case RedisChannels.WebhookRaid:
                    var raid = JsonSerializer.Deserialize<Gym>(message);
                    if (raid == null) return;
                    WebhookController.Instance.AddRaid(raid);
                    break;
                case RedisChannels.WebhookEgg:
                    var egg = JsonSerializer.Deserialize<Gym>(message);
                    if (egg == null) return;
                    WebhookController.Instance.AddEgg(egg);
                    break;
                case RedisChannels.WebhookGym:
                    var gym = JsonSerializer.Deserialize<Gym>(message);
                    if (gym == null) return;
                    WebhookController.Instance.AddGym(gym);
                    break;
                case RedisChannels.WebhookGymDefender:
                case RedisChannels.WebhookGymTrainer:
                    // TODO: Gym defenders and trainers
                    break;
                case RedisChannels.WebhookLure:
                    var lure = JsonSerializer.Deserialize<Pokestop>(message);
                    if (lure == null) return;
                    WebhookController.Instance.AddLure(lure);
                    break;
                case RedisChannels.WebhookInvasion:
                    var invasion = JsonSerializer.Deserialize<Pokestop>(message);
                    if (invasion == null) return;
                    WebhookController.Instance.AddInvasion(invasion);
                    break;
                case RedisChannels.WebhookPokestop:
                    var pokestop = JsonSerializer.Deserialize<Pokestop>(message);
                    if (pokestop == null) return;
                    WebhookController.Instance.AddPokestop(pokestop);
                    break;
                case RedisChannels.WebhookQuest:
                    var quest = JsonSerializer.Deserialize<Pokestop>(message);
                    if (quest == null) return;
                    WebhookController.Instance.AddQuest(quest);
                    break;
                case RedisChannels.WebhookWeather:
                    var weather = JsonSerializer.Deserialize<Weather>(message);
                    if (weather == null) return;
                    WebhookController.Instance.AddWeather(weather);
                    break;
                case RedisChannels.WebhookAccount:
                    // TODO: Account bans and warnings
                    break;
            }
        }
    }
}