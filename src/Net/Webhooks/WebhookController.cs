namespace ChuckDeviceController.Net.Webhooks
{
    using System;
    using System.Collections.Generic;
    using System.Threading;

    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Extensions;
    using ChuckDeviceController.Services.Queues;
    using ChuckDeviceController.Utilities;

    public class WebhookController
    {
        #region Variables

        // TODO: Use only one queue and one lock of dynamic type
        //private readonly IQueue<dynamic> _eventsQueue = new WebhookQueue<dynamic>();

        private readonly List<dynamic> _sentEvents;

        private readonly IQueue<Pokemon> _pokemonQueue;
        private readonly IQueue<Gym> _gymQueue;
        private readonly IQueue<Gym> _gymInfoQueue;
        private readonly IQueue<Gym> _eggQueue;
        private readonly IQueue<Gym> _raidQueue;
        private readonly IQueue<Pokestop> _pokestopQueue;
        private readonly IQueue<Pokestop> _lureQueue;
        private readonly IQueue<Pokestop> _invasionQueue;
        private readonly IQueue<Pokestop> _questQueue;
        private readonly IQueue<Weather> _weatherQueue;

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

        private Thread _thread;
        //private readonly System.Timers.Timer _timer;

        #endregion

        #region Properties

        public bool IsRunning { get; set; }

        public ushort SleepIntervalS { get; set; }

        #region Singleton

        private static WebhookController _instance;
        public static WebhookController Instance =>
            _instance ??= new WebhookController();

        #endregion

        #endregion

        #region Constructor

        public WebhookController()
        {
            _pokemonQueue = new WebhookQueue<Pokemon>();
            _gymQueue = new WebhookQueue<Gym>();
            _gymInfoQueue = new WebhookQueue<Gym>();
            _eggQueue = new WebhookQueue<Gym>();
            _raidQueue = new WebhookQueue<Gym>();
            _pokestopQueue = new WebhookQueue<Pokestop>();
            _lureQueue = new WebhookQueue<Pokestop>();
            _invasionQueue = new WebhookQueue<Pokestop>();
            _questQueue = new WebhookQueue<Pokestop>();
            _weatherQueue = new WebhookQueue<Weather>();

            _sentEvents = new List<dynamic>();

            /*
            _timer = new System.Timers.Timer
            {
                Interval = 5 * 1000, // TODO: Configurable
            };
            _timer.Elapsed += (sender, e) => OnTimeElapsed();
            */
            SleepIntervalS = 5;
        }

        #endregion

        #region Public Methods

        public void Start()
        {
            //_timer.Start();
            IsRunning = true;
            if (_thread == null)
            {
                _thread = new Thread(_ => OnTimeElapsed())
                {
                    IsBackground = true,
                };
            }
            _thread.Start();
        }

        public void Stop()
        {
            //_timer.Stop();
            IsRunning = false;
            if (_thread == null)
                return;

            _thread.Interrupt();
            if (!_thread.Join(2000))
            {
                ConsoleExt.WriteError($"[WebhookController] Failed to abort webhook thread");
            }
            _thread = null;
        }

        #region Add

        #region Pokemon

        public void AddPokemon(Pokemon pokemon)
        {
            if (Startup.Config.Webhooks.Count == 0)
                return;

            lock (_pokemonLock)
            {
                if (!_sentEvents.Contains(pokemon))
                {
                    _pokemonQueue.Enqueue(pokemon);
                }
            }
        }

        public void AddPokemon(IEnumerable<Pokemon> pokemon)
        {
            if (Startup.Config.Webhooks.Count == 0)
                return;

            foreach (var pkmn in pokemon)
            {
                AddPokemon(pkmn);
            }
        }

        #endregion

        #region Gyms

        public void AddGym(Gym gym)
        {
            if (Startup.Config.Webhooks.Count == 0)
                return;

            lock (_gymLock)
            {
                if (!_sentEvents.Contains(gym))
                {
                    _gymQueue.Enqueue(gym);
                }
            }
        }

        public void AddEgg(Gym gym)
        {
            if (Startup.Config.Webhooks.Count == 0)
                return;

            lock (_eggLock)
            {
                if (!_sentEvents.Contains(gym))
                {
                    _eggQueue.Enqueue(gym);
                }
            }
        }

        public void AddRaid(Gym gym)
        {
            if (Startup.Config.Webhooks.Count == 0)
                return;

            lock (_raidLock)
            {
                if (!_sentEvents.Contains(gym))
                {
                    _raidQueue.Enqueue(gym);
                }
            }
        }

        public void AddGymInfo(Gym gym)
        {
            if (Startup.Config.Webhooks.Count == 0)
                return;

            lock (_gymInfoLock)
            {
                if (!_sentEvents.Contains(gym))
                {
                    _gymInfoQueue.Enqueue(gym);
                }
            }
        }

        #endregion

        #region Pokestops

        public void AddPokestop(Pokestop pokestop)
        {
            if (Startup.Config.Webhooks.Count == 0)
                return;

            lock (_pokestopLock)
            {
                if (!_sentEvents.Contains(pokestop))
                {
                    _pokestopQueue.Enqueue(pokestop);
                }
            }
        }

        public void AddLure(Pokestop pokestop)
        {
            if (Startup.Config.Webhooks.Count == 0)
                return;

            lock (_lureLock)
            {
                if (!_sentEvents.Contains(pokestop))
                {
                    _lureQueue.Enqueue(pokestop);
                }
            }
        }

        public void AddInvasion(Pokestop pokestop)
        {
            if (Startup.Config.Webhooks.Count == 0)
                return;

            lock (_invasionLock)
            {
                if (!_sentEvents.Contains(pokestop))
                {
                    _invasionQueue.Enqueue(pokestop);
                }
            }
        }

        #endregion

        #region Quests

        public void AddQuest(Pokestop pokestop)
        {
            if (Startup.Config.Webhooks.Count == 0)
                return;

            lock (_questLock)
            {
                if (!_sentEvents.Contains(pokestop))
                {
                    _questQueue.Enqueue(pokestop);
                }
            }
        }

        public void AddQuests(IEnumerable<Pokestop> quests)
        {
            if (Startup.Config.Webhooks.Count == 0)
                return;

            foreach (var quest in quests)
            {
                AddQuest(quest);
            }
        }

        #endregion

        #region Weather

        public void AddWeather(Weather weather)
        {
            if (Startup.Config.Webhooks.Count == 0)
                return;

            lock (_weatherLock)
            {
                if (!_sentEvents.Contains(weather))
                {
                    _weatherQueue.Enqueue(weather);
                }
            }
        }

        public void AddWeather(IEnumerable<Weather> weather)
        {
            if (Startup.Config.Webhooks.Count == 0)
                return;

            foreach (var item in weather)
            {
                AddWeather(item);
            }
        }

        #endregion

        #endregion

        #endregion

        #region Private Methods

        private void OnTimeElapsed()
        {
            while (IsRunning)
            {
                var events = new List<dynamic>();
                if (_pokemonQueue.Count > 0)
                {
                    lock (_pokemonLock)
                    {
                        for (var i = 0; i < _pokemonQueue.Count; i++)
                        {
                            var item = _pokemonQueue.Dequeue();
                            _sentEvents.Add(item);
                            events.Add(item.GetWebhookValues("pokemon"));
                            Thread.Sleep(5);
                        }
                    }
                }

                if (_gymQueue.Count > 0)
                {
                    lock (_gymLock)
                    {
                        for (var i = 0; i < _gymQueue.Count; i++)
                        {
                            var item = _gymQueue.Dequeue();
                            _sentEvents.Add(item);
                            events.Add(item.GetWebhookValues("gym"));
                            Thread.Sleep(5);
                        }
                    }
                }

                if (_gymInfoQueue.Count > 0)
                {
                    lock (_gymInfoLock)
                    {
                        for (var i = 0; i < _gymInfoQueue.Count; i++)
                        {
                            var item = _gymInfoQueue.Dequeue();
                            _sentEvents.Add(item);
                            events.Add(item.GetWebhookValues("gym-info"));
                            Thread.Sleep(5);
                        }
                    }
                }

                if (_eggQueue.Count > 0)
                {
                    lock (_eggLock)
                    {
                        for (var i = 0; i < _eggQueue.Count; i++)
                        {
                            var item = _eggQueue.Dequeue();
                            _sentEvents.Add(item);
                            events.Add(item.GetWebhookValues("egg"));
                            Thread.Sleep(5);
                        }
                    }
                }

                if (_raidQueue.Count > 0)
                {
                    lock (_raidLock)
                    {
                        for (var i = 0; i < _raidQueue.Count; i++)
                        {
                            var item = _raidQueue.Dequeue();
                            _sentEvents.Add(item);
                            events.Add(item.GetWebhookValues("raid"));
                            Thread.Sleep(5);
                        }
                    }
                }

                if (_pokestopQueue.Count > 0)
                {
                    lock (_pokestopLock)
                    {
                        for (var i = 0; i < _pokestopQueue.Count; i++)
                        {
                            var item = _pokestopQueue.Dequeue();
                            _sentEvents.Add(item);
                            events.Add(item.GetWebhookValues("pokestop"));
                            Thread.Sleep(5);
                        }
                    }
                }

                if (_lureQueue.Count > 0)
                {
                    lock (_lureLock)
                    {
                        for (var i = 0; i < _lureQueue.Count; i++)
                        {
                            var item = _lureQueue.Dequeue();
                            _sentEvents.Add(item);
                            events.Add(item.GetWebhookValues("lure"));
                            Thread.Sleep(5);
                        }
                    }
                }

                if (_invasionQueue.Count > 0)
                {
                    lock (_invasionLock)
                    {
                        for (var i = 0; i < _invasionQueue.Count; i++)
                        {
                            var item = _invasionQueue.Dequeue();
                            _sentEvents.Add(item);
                            events.Add(item.GetWebhookValues("invasion"));
                            Thread.Sleep(5);
                        }
                    }
                }

                if (_questQueue.Count > 0)
                {
                    lock (_questLock)
                    {
                        for (var i = 0; i < _questQueue.Count; i++)
                        {
                            var item = _questQueue.Dequeue();
                            _sentEvents.Add(item);
                            events.Add(item.GetWebhookValues("quest"));
                            Thread.Sleep(5);
                        }
                    }
                }

                if (_weatherQueue.Count > 0)
                {
                    lock (_weatherLock)
                    {
                        for (var i = 0; i < _weatherQueue.Count; i++)
                        {
                            var item = _weatherQueue.Dequeue();
                            _sentEvents.Add(item);
                            events.Add(item.GetWebhookValues("weather"));
                            Thread.Sleep(5);
                        }
                    }
                }

                if (events.Count == 0)
                {
                    Thread.Sleep(SleepIntervalS * 1000);
                    continue;
                }

                foreach (var url in Startup.Config.Webhooks)
                {
                    SendEvents(url, events);
                }

                Thread.Sleep(SleepIntervalS * 1000);
            }
        }

        private static bool SendEvents(string url, List<dynamic> events, ushort retryCount = 0)
        {
            if (events == null || events.Count == 0)
                return false;

            NetUtil.SendWebhook(url, events.ToJson(), retryCount);
            ConsoleExt.WriteInfo($"[WebhookController] Sent {events.Count} webhook events to {url}");
            return true;
        }

        #endregion
    }
}