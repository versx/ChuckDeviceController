namespace WebhookProcessor
{
    using System;
    using System.Collections.Generic;
    using System.Threading;

    using Chuck.Common.Utilities;
    using Chuck.Data.Entities;
    using Chuck.Extensions;
    using WebhookProcessor.Queues;

    public class WebhookController
    {
        #region Variables

        private readonly IQueue<Account> _accountQueue;
        private readonly IQueue<Pokemon> _pokemonQueue;
        private readonly IQueue<Gym> _eggQueue;
        private readonly IQueue<Gym> _raidQueue;
        private readonly IQueue<Gym> _gymQueue;
        private readonly IQueue<GymDefender> _gymDefenderQueue;
        private readonly IQueue<Trainer> _gymTrainerQueue;
        private readonly IQueue<Pokestop> _pokestopQueue;
        private readonly IQueue<Pokestop> _lureQueue;
        private readonly IQueue<Pokestop> _invasionQueue;
        private readonly IQueue<Pokestop> _questQueue;
        private readonly IQueue<Weather> _weatherQueue;

        //private readonly List<object> _sentEvents;

        private readonly object _webhooksLock = new object();

        private readonly object _accountLock = new object();
        private readonly object _pokemonLock = new object();
        private readonly object _eggLock = new object();
        private readonly object _raidLock = new object();
        private readonly object _gymLock = new object();
        private readonly object _gymDefenderLock = new object();
        private readonly object _gymTrainerLock = new object();
        private readonly object _pokestopLock = new object();
        private readonly object _lureLock = new object();
        private readonly object _invasionLock = new object();
        private readonly object _questLock = new object();
        private readonly object _weatherLock = new object();
        //private readonly object _lock = new object();

        private Thread _thread;
        //private readonly System.Timers.Timer _timer;

        #endregion

        #region Singleton

        private static WebhookController _instance;
        public static WebhookController Instance =>
            _instance ??= new WebhookController();

        #endregion

        #region Properties

        public bool IsRunning { get; private set; }

        public ushort SleepIntervalS { get; set; }

        public IReadOnlyList<Webhook> Webhooks { get; set; }

        #endregion

        #region Constructor

        public WebhookController()
        {
            //_sentEvents = new List<object>();
            _accountQueue = new WebhookQueue<Account>();
            _pokemonQueue = new WebhookQueue<Pokemon>();
            _eggQueue = new WebhookQueue<Gym>();
            _raidQueue = new WebhookQueue<Gym>();
            _gymQueue = new WebhookQueue<Gym>();
            _gymDefenderQueue = new WebhookQueue<GymDefender>();
            _gymTrainerQueue = new WebhookQueue<Trainer>();
            _pokestopQueue = new WebhookQueue<Pokestop>();
            _lureQueue = new WebhookQueue<Pokestop>();
            _invasionQueue = new WebhookQueue<Pokestop>();
            _questQueue = new WebhookQueue<Pokestop>();
            _weatherQueue = new WebhookQueue<Weather>();
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

        public void AddAccount(Account account)
        {
            if (Webhooks?.Count == 0)
                return;

            //if (!_sentEvents.Contains(account))
            {
                lock (_accountLock)
                {
                    _accountQueue.Enqueue(account);
                }
            }
        }

        public void AddPokemon(Pokemon pokemon)
        {
            if (Webhooks?.Count == 0)
                return;

            //if (!_sentEvents.Contains(pokemon))
            {
                lock (_pokemonLock)
                {
                    _pokemonQueue.Enqueue(pokemon);
                }
            }
        }

        public void AddEgg(Gym egg)
        {
            if (Webhooks?.Count == 0)
                return;

            //if (!_sentEvents.Contains(egg))
            {
                lock (_eggLock)
                {
                    _eggQueue.Enqueue(egg);
                }
            }
        }

        public void AddRaid(Gym raid)
        {
            if (Webhooks?.Count == 0)
                return;

            //if (!_sentEvents.Contains(raid))
            {
                lock (_raidLock)
                {
                    _raidQueue.Enqueue(raid);
                }
            }
        }

        public void AddGym(Gym gym)
        {
            if (Webhooks?.Count == 0)
                return;

            //if (!_sentEvents.Contains(gym))
            {
                lock (_gymLock)
                {
                    _gymQueue.Enqueue(gym);
                }
            }
        }

        public void AddGymDefender(GymDefender gymDefender)
        {
            if (Webhooks?.Count == 0)
                return;

            //if (!_sentEvents.Contains(gymDefender))
            {
                lock (_gymDefenderLock)
                {
                    _gymDefenderQueue.Enqueue(gymDefender);
                }
            }
        }

        public void AddGymTrainer(Trainer gymTrainer)
        {
            if (Webhooks?.Count == 0)
                return;

            //if (!_sentEvents.Contains(gymTrainer))
            {
                lock (_gymTrainerLock)
                {
                    _gymTrainerQueue.Enqueue(gymTrainer);
                }
            }
        }

        public void AddPokestop(Pokestop pokestop)
        {
            if (Webhooks?.Count == 0)
                return;

            //if (!_sentEvents.Contains(pokestop))
            {
                lock (_pokestopLock)
                {
                    _pokestopQueue.Enqueue(pokestop);
                }
            }
        }

        public void AddLure(Pokestop lure)
        {
            if (Webhooks?.Count == 0)
                return;

            //if (!_sentEvents.Contains(lure))
            {
                lock (_lureLock)
                {
                    _lureQueue.Enqueue(lure);
                }
            }
        }

        public void AddInvasion(Pokestop invasion)
        {
            if (Webhooks?.Count == 0)
                return;

            //if (!_sentEvents.Contains(invasion))
            {
                lock (_invasionLock)
                {
                    _invasionQueue.Enqueue(invasion);
                }
            }
        }

        public void AddQuest(Pokestop quest)
        {
            if (Webhooks?.Count == 0)
                return;

            //if (!_sentEvents.Contains(quest))
            {
                lock (_questLock)
                {
                    _questQueue.Enqueue(quest);
                }
            }
        }

        public void AddWeather(Weather weather)
        {
            if (Webhooks?.Count == 0)
                return;

            //if (!_sentEvents.Contains(weather))
            {
                lock (_weatherLock)
                {
                    _weatherQueue.Enqueue(weather);
                }
            }
        }

        #endregion

        public void SetWebhooks(List<Webhook> webhooks)
        {
            Webhooks = webhooks;
        }

        #endregion

        #region Private Methods

        private void OnTimeElapsed()
        {
            while (IsRunning)
            {
                var events = new List<dynamic>();
                if (Webhooks == null)
                {
                    Thread.Sleep(SleepIntervalS * 1000);
                    continue;
                }

                foreach (var webhook in Webhooks)
                {
                    // Skip webhooks not enabled
                    if (!webhook.Enabled)
                        continue;

                    // Check account webhooks against webhook filters
                    /*
                    if (_accountQueue.Count > 0 && webhook.Types.Contains(WebhookType.Account))
                    {
                        lock (_accountLock)
                        {
                            while (_accountQueue.Count > 0)
                            {
                                var account = _accountQueue.Dequeue();
                                events.Add(account.GetWebhookValues("account"));
                                Thread.Sleep(1);
                            }
                        }
                    }
                    */
                    // Check pokemon webhooks against webhook filters
                    if (_pokemonQueue.Count > 0 && webhook.Types.Contains(WebhookType.Pokemon))
                    {
                        var pokemonEvents = new List<Pokemon>();
                        lock (_pokemonLock)
                        {
                            while (_pokemonQueue.Count > 0)
                            {
                                pokemonEvents.Add(_pokemonQueue.Dequeue());
                                Thread.Sleep(1);
                            }
                        }
                        foreach (var pokemon in pokemonEvents)
                        {
                            // TODO: Check geofence area
                            if (webhook.Data.PokemonIds?.Count == 0 || !webhook.Data.PokemonIds.Contains(pokemon.PokemonId))
                            {
                                events.Add(pokemon.GetWebhookValues("pokemon"));
                            }
                        }
                    }
                    // Check egg webhooks against webhook filters
                    if (_eggQueue.Count > 0 && webhook.Types.Contains(WebhookType.Eggs))
                    {
                        var eggEvents = new List<Gym>();
                        lock (_eggLock)
                        {
                            while (_eggQueue.Count > 0)
                            {
                                eggEvents.Add(_eggQueue.Dequeue());
                                Thread.Sleep(1);
                            }
                        }
                        foreach (var egg in eggEvents)
                        {
                            if (webhook.Data.EggLevels?.Count == 0 || !webhook.Data.EggLevels.Contains(egg.RaidLevel))
                            {
                                events.Add(egg.GetWebhookValues("egg"));
                            }
                        }
                    }
                    // Check raid webhooks against webhook filters
                    if (_raidQueue.Count > 0 && webhook.Types.Contains(WebhookType.Raids))
                    {
                        var raidEvents = new List<Gym>();
                        lock (_raidLock)
                        {
                            while (_raidQueue.Count > 0)
                            {
                                raidEvents.Add(_raidQueue.Dequeue());
                                Thread.Sleep(1);
                            }
                        }
                        foreach (var raid in raidEvents)
                        {
                            if (webhook.Data.RaidPokemonIds?.Count == 0 || !webhook.Data.RaidPokemonIds.Contains(raid.RaidPokemonId ?? 0))
                            {
                                events.Add(raid.GetWebhookValues("raid"));
                            }
                        }
                    }
                    // Check gym webhooks against webhook filters
                    if (_gymQueue.Count > 0 && webhook.Types.Contains(WebhookType.Gyms))
                    {
                        var gymEvents = new List<Gym>();
                        lock (_gymLock)
                        {
                            while (_gymQueue.Count > 0)
                            {
                                gymEvents.Add(_gymQueue.Dequeue());
                                Thread.Sleep(1);
                            }
                        }
                        foreach (var gym in gymEvents)
                        {
                            if (webhook.Data.GymTeamIds?.Count == 0 || !webhook.Data.GymTeamIds.Contains((ushort)gym.Team))
                            {
                                events.Add(gym.GetWebhookValues("gym"));
                            }
                        }
                    }
                    // Check gym defender webhooks against webhook filters
                    if (_gymDefenderQueue.Count > 0) // TODO: WebhookType.GymDefenders
                    {
                        var gymDefenderEvents = new List<GymDefender>();
                        lock (_gymDefenderLock)
                        {
                            while (_gymDefenderQueue.Count > 0)
                            {
                                gymDefenderEvents.Add(_gymDefenderQueue.Dequeue());
                                Thread.Sleep(1);
                            }
                        }
                        foreach (var defender in gymDefenderEvents)
                        {
                            // TODO: Defender filters
                            events.Add(defender.GetWebhookValues("gym_defender"));
                        }
                    }
                    // Check gym trainer webhooks against webhook filters
                    if (_gymTrainerQueue.Count > 0) // TODO: WebhookType.GymTrainers
                    {
                        var gymTrainerEvents = new List<Trainer>();
                        lock (_gymTrainerLock)
                        {
                            while (_gymTrainerQueue.Count > 0)
                            {
                                gymTrainerEvents.Add(_gymTrainerQueue.Dequeue());
                                Thread.Sleep(1);
                            }
                        }
                        foreach (var trainer in gymTrainerEvents)
                        {
                            // TODO: Trainer filters
                            events.Add(trainer.GetWebhookValues("gym_trainer"));
                        }
                    }
                    // Check pokestop webhooks against webhook filters
                    if (_pokestopQueue.Count > 0 && webhook.Types.Contains(WebhookType.Pokestops))
                    {
                        var pokestopEvents = new List<Pokestop>();
                        lock (_pokestopLock)
                        {
                            while (_pokestopQueue.Count > 0)
                            {
                                pokestopEvents.Add(_pokestopQueue.Dequeue());
                                Thread.Sleep(1);
                            }
                        }
                        foreach (var pokestop in pokestopEvents)
                        {
                            if (webhook.Data.PokestopIds?.Count == 0 || !webhook.Data.PokestopIds.Contains(pokestop.Id))
                            {
                                events.Add(pokestop.GetWebhookValues("pokestop"));
                            }
                        }
                    }
                    // Check lure webhooks against webhook filters
                    if (_lureQueue.Count > 0 && webhook.Types.Contains(WebhookType.Lures))
                    {
                        var lureEvents = new List<Pokestop>();
                        lock (_lureLock)
                        {
                            while (_lureQueue.Count > 0)
                            {
                                lureEvents.Add(_lureQueue.Dequeue());
                                Thread.Sleep(1);
                            }
                        }
                        foreach (var lure in lureEvents)
                        {
                            if (webhook.Data.LureIds?.Count == 0 || !webhook.Data.LureIds.Contains((ushort)lure.LureId))
                            {
                                events.Add(lure.GetWebhookValues("lure"));
                            }
                        }
                    }
                    // Check invasion webhooks against webhook filters
                    if (_invasionQueue.Count > 0 && webhook.Types.Contains(WebhookType.Invasions))
                    {
                        var invasionEvents = new List<Pokestop>();
                        lock (_invasionLock)
                        {
                            while (_invasionQueue.Count > 0)
                            {
                                invasionEvents.Add(_invasionQueue.Dequeue());
                                Thread.Sleep(1);
                            }
                        }
                        foreach (var invasion in invasionEvents)
                        {
                            if (webhook.Data.InvasionIds?.Count == 0 || !webhook.Data.InvasionIds.Contains((ushort)invasion.GruntType))
                            {
                                events.Add(invasion.GetWebhookValues("invasion"));
                            }
                        }
                    }
                    // Check quest webhooks against webhook filters
                    if (_questQueue.Count > 0 && webhook.Types.Contains(WebhookType.Quests))
                    {
                        var questEvents = new List<Pokestop>();
                        lock (_questLock)
                        {
                            while (_questQueue.Count > 0)
                            {
                                questEvents.Add(_questQueue.Dequeue());
                                Thread.Sleep(1);
                            }
                        }
                        foreach (var quest in questEvents)
                        {
                            events.Add(quest.GetWebhookValues("quest"));
                        }
                    }
                    // Check weather webhooks against webhook filters
                    if (_weatherQueue.Count > 0 && webhook.Types.Contains(WebhookType.Weather))
                    {
                        var weatherEvents = new List<Weather>();
                        lock (_weatherLock)
                        {
                            while (_weatherQueue.Count > 0)
                            {
                                weatherEvents.Add(_weatherQueue.Dequeue());
                                Thread.Sleep(1);
                            }
                        }
                        foreach (var weather in weatherEvents)
                        {
                            if (webhook.Data.WeatherConditionIds?.Count == 0 || !webhook.Data.WeatherConditionIds.Contains((ushort)weather.GameplayCondition))
                            {
                                events.Add(weather.GetWebhookValues("weather"));
                            }
                        }
                    }

                    if (events.Count == 0)
                    {
                        Thread.Sleep(SleepIntervalS * 1000);
                        continue;
                    }

                    // Send all filtered events to webhook url
                    SendEvents(webhook.Url, events);
                    //_sentEvents.AddRange(events);
                    Thread.Sleep(Convert.ToInt32(webhook.Delay * 1000));
                }
            }
        }

        private static bool SendEvents(string url, List<dynamic> events, ushort retryCount = 0)
        {
            if (events == null || events.Count == 0)
                return false;

            NetUtils.SendWebhook(url, events.ToJson(), retryCount);
            ConsoleExt.WriteInfo($"[WebhookController] Sent {events.Count} webhook events to {url}");
            return true;
        }

        #endregion
    }
}