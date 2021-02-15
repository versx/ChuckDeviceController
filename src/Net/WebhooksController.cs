namespace ChuckDeviceController.Net
{
    using System;
    using System.Collections.Generic;

    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Extensions;
    using ChuckDeviceController.Services.Queues;
    using ChuckDeviceController.Utilities;

    public class WebhookQueue<T> : BaseQueue<T> where T: BaseEntity
    {
    }

    public class WebhooksController
    {
        // TODO: Add entity queues
        // TODO: Loops queues and send payloads to webhook endpoints

        // TODO: Use only one queue and one lock of dynamic type

        //private readonly IQueue<dynamic> _eventsQueue = new WebhookQueue<dynamic>();

        private readonly IQueue<Pokemon> _pokemonQueue = new WebhookQueue<Pokemon>();
        private readonly IQueue<Gym> _gymQueue = new WebhookQueue<Gym>();
        private readonly IQueue<Gym> _gymInfoQueue = new WebhookQueue<Gym>();
        private readonly IQueue<Gym> _eggQueue = new WebhookQueue<Gym>();
        private readonly IQueue<Gym> _raidQueue = new WebhookQueue<Gym>();
        private readonly IQueue<Pokestop> _pokestopQueue = new WebhookQueue<Pokestop>();
        private readonly IQueue<Pokestop> _lureQueue = new WebhookQueue<Pokestop>();
        private readonly IQueue<Pokestop> _invasionQueue = new WebhookQueue<Pokestop>();
        private readonly IQueue<Pokestop> _questQueue = new WebhookQueue<Pokestop>();
        private readonly IQueue<Weather> _weatherQueue = new WebhookQueue<Weather>();

        private static readonly object _pokemonLock = new object();
        private static readonly object _gymLock = new object();
        private static readonly object _gymInfoLock = new object();
        private static readonly object _eggLock = new object();
        private static readonly object _raidLock = new object();
        private static readonly object _pokestopLock = new object();
        private static readonly object _lureLock = new object();
        private static readonly object _invasionLock = new object();
        private static readonly object _questLock = new object();
        private static readonly object _weatherLock = new object();

        private readonly System.Timers.Timer _timer;

        public WebhooksController()
        {
            _timer = new System.Timers.Timer
            {
                Interval = 5 * 1000, // TODO: Configurable
            };
            _timer.Elapsed += (sender, e) => OnTimeElapsed();
            _timer.Start();
        }

        #region Add To Queue

        public void AddPokemon(Pokemon pokemon)
        {
            lock (_pokemonLock)
            {
                _pokemonQueue.Enqueue(pokemon);
            }
        }

        public void AddPokemon(IEnumerable<Pokemon> pokemon)
        {
            foreach (var pkmn in pokemon)
            {
                AddPokemon(pkmn);
            }
        }

        public void AddGym(Gym gym)
        {
            lock (_gymLock)
            {
                _gymQueue.Enqueue(gym);
            }
        }

        public void AddEgg(Gym gym)
        {
            lock (_eggLock)
            {
                _eggQueue.Enqueue(gym);
            }
        }

        public void AddRaid(Gym gym)
        {
            lock (_raidLock)
            {
                _raidQueue.Enqueue(gym);
            }
        }

        public void AddGymInfo(Gym gym)
        {
            lock (_gymInfoLock)
            {
                _gymInfoQueue.Enqueue(gym);
            }
        }

        public void AddPokestop(Pokestop pokestop)
        {
            lock (_pokestopLock)
            {
                _pokestopQueue.Enqueue(pokestop);
            }
        }

        public void AddLure(Pokestop pokestop)
        {
            lock (_lureLock)
            {
                _lureQueue.Enqueue(pokestop);
            }
        }

        public void AddInvasion(Pokestop pokestop)
        {
            lock (_invasionLock)
            {
                _invasionQueue.Enqueue(pokestop);
            }
        }

        public void AddQuest(Pokestop pokestop)
        {
            lock (_questLock)
            {
                _questQueue.Enqueue(pokestop);
            }
        }

        public void AddWeather(Weather weather)
        {
            lock (_weatherLock)
            {
                _weatherQueue.Enqueue(weather);
            }
        }

        #endregion

        private void OnTimeElapsed()
        {
            var events = new List<dynamic>();
            if (_pokemonQueue.Count > 0)
            {
                lock (_pokemonLock)
                {
                    while (_pokemonQueue.Count > 0)
                    {
                        events.Add(_pokemonQueue.Dequeue().GetWebhookValues("pokemon"));
                    }
                }
            }

            if (_gymQueue.Count > 0)
            {
                lock (_gymLock)
                {
                    while (_gymQueue.Count > 0)
                    {
                        events.Add(_gymQueue.Dequeue().GetWebhookValues("gym"));
                    }
                }
            }

            if (_gymInfoQueue.Count > 0)
            {
                lock (_gymInfoLock)
                {
                    while (_gymInfoQueue.Count > 0)
                    {
                        events.Add(_gymInfoQueue.Dequeue().GetWebhookValues("gym-info"));
                    }
                }
            }

            if (_eggQueue.Count > 0)
            {
                lock (_eggLock)
                {
                    while (_eggQueue.Count > 0)
                    {
                        events.Add(_eggQueue.Dequeue().GetWebhookValues("egg"));
                    }
                }
            }

            if (_raidQueue.Count > 0)
            {
                lock (_raidLock)
                {
                    while (_raidQueue.Count > 0)
                    {
                        events.Add(_raidQueue.Dequeue().GetWebhookValues("raid"));
                    }
                }
            }

            if (_pokestopQueue.Count > 0)
            {
                lock (_pokestopLock)
                {
                    while (_pokestopQueue.Count > 0)
                    {
                        events.Add(_pokestopQueue.Dequeue().GetWebhookValues("pokestop"));
                    }
                }
            }

            if (_lureQueue.Count > 0)
            {
                lock (_lureLock)
                {
                    while (_lureQueue.Count > 0)
                    {
                        events.Add(_lureQueue.Dequeue().GetWebhookValues("lure"));
                    }
                }
            }

            if (_invasionQueue.Count > 0)
            {
                lock (_invasionLock)
                {
                    while (_invasionQueue.Count > 0)
                    {
                        events.Add(_invasionQueue.Dequeue().GetWebhookValues("invasion"));
                    }
                }
            }

            if (_questQueue.Count > 0)
            {
                lock (_questLock)
                {
                    while (_questQueue.Count > 0)
                    {
                        events.Add(_questQueue.Dequeue().GetWebhookValues("quest"));
                    }
                }
            }

            if (_weatherQueue.Count > 0)
            {
                lock (_weatherLock)
                {
                    while (_weatherQueue.Count > 0)
                    {
                        events.Add(_weatherQueue.Dequeue().GetWebhookValues("weather"));
                    }
                }
            }

            if (events.Count == 0)
                return;

            foreach (var url in Startup.Config.Webhooks)
            {
                SendEvents(url, events);
                // TODO: Sleep 5 seconds
            }
        }

        private static bool SendEvents(string url, List<dynamic> events, ushort retryCount = 0)
        {
            if (events == null || events.Count == 0)
                return false;

            NetUtil.SendWebhook(url, events.ToJson(), retryCount);
            return true;
        }
    }
}