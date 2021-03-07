namespace WebhookProcessor
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;

    using StackExchange.Redis;

    using Chuck.Infrastructure.Common;
    using Chuck.Infrastructure.Configuration;
    using Chuck.Infrastructure.Data.Entities;
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
                    _subscriber.Subscribe(/*_config.Redis.QueueName*/"*", SubscriptionHandler);
                    //_redisDatabase = _redis.GetDatabase(_config.Redis.DatabaseNum);
                }

                ConsoleExt.WriteInfo($"[WebhookProcessor] Starting...");
                WebhookController.Instance.SleepIntervalS = 5;
                WebhookController.Instance.Start();
                ConsoleExt.WriteInfo($"[WebhookProcessor] Started, waiting for webhook events");
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

            //ConsoleExt.WriteDebug($"[WebhookProcessor] [{channel}] Received: {message}");
            switch (channel)
            {
                case RedisChannels.WebhookAccount:
                    var account = message.ToString().FromJson<Account>();
                    if (account == null) return;
                    ThreadPool.QueueUserWorkItem(_ => WebhookController.Instance.AddAccount(account));
                    break;
                case RedisChannels.WebhookEgg:
                    var egg = message.ToString().FromJson<Gym>();
                    if (egg == null) return;
                    ThreadPool.QueueUserWorkItem(_ => WebhookController.Instance.AddEgg(egg));
                    break;
                case RedisChannels.WebhookGym:
                    var gym = message.ToString().FromJson<Gym>();
                    if (gym == null) return;
                    ThreadPool.QueueUserWorkItem(_ => WebhookController.Instance.AddGym(gym));
                    break;
                case RedisChannels.WebhookGymDefender:
                    var gymDefender = message.ToString().FromJson<GymDefender>();
                    if (gymDefender == null) return;
                    ThreadPool.QueueUserWorkItem(_ => WebhookController.Instance.AddGymDefender(gymDefender));
                    break;
                case RedisChannels.WebhookGymTrainer:
                    var gymTrainer = message.ToString().FromJson<Trainer>();
                    if (gymTrainer == null) return;
                    ThreadPool.QueueUserWorkItem(_ => WebhookController.Instance.AddGymTrainer(gymTrainer));
                    break;
                case RedisChannels.WebhookInvasion:
                    var invasion = message.ToString().FromJson<Pokestop>();
                    if (invasion == null) return;
                    ThreadPool.QueueUserWorkItem(_ => WebhookController.Instance.AddInvasion(invasion));
                    break;
                case RedisChannels.WebhookLure:
                    var lure = message.ToString().FromJson<Pokestop>();
                    if (lure == null) return;
                    ThreadPool.QueueUserWorkItem(_ => WebhookController.Instance.AddLure(lure));
                    break;
                case RedisChannels.WebhookPokemon:
                    var pokemon = message.ToString().FromJson<Pokemon>();
                    if (pokemon == null) return;
                    ThreadPool.QueueUserWorkItem(_ => WebhookController.Instance.AddPokemon(pokemon));
                    break;
                case RedisChannels.WebhookPokestop:
                    var pokestop = message.ToString().FromJson<Pokestop>();
                    if (pokestop == null) return;
                    ThreadPool.QueueUserWorkItem(_ => WebhookController.Instance.AddPokestop(pokestop));
                    break;
                case RedisChannels.WebhookQuest:
                    var quest = message.ToString().FromJson<Pokestop>();
                    if (quest == null) return;
                    ThreadPool.QueueUserWorkItem(_ => WebhookController.Instance.AddQuest(quest));
                    break;
                case RedisChannels.WebhookRaid:
                    var raid = message.ToString().FromJson<Gym>();
                    if (raid == null) return;
                    ThreadPool.QueueUserWorkItem(_ => WebhookController.Instance.AddRaid(raid));
                    break;
                case RedisChannels.WebhookWeather:
                    var weather = message.ToString().FromJson<Weather>();
                    if (weather == null) return;
                    ThreadPool.QueueUserWorkItem(_ => WebhookController.Instance.AddWeather(weather));
                    break;

                case RedisChannels.WebhookReload:
                    var webhooks = message.ToString().FromJson<List<Webhook>>();
                    if (webhooks == null) return;
                    WebhookController.Instance.SetWebhooks(webhooks);
                    break;
            }
        }
    }
}