namespace ChuckDeviceCommunicator.Services
{
    using System.Timers;

    using ChuckDeviceCommunicator.Services.Rpc;
    using ChuckDeviceController.Data;
    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Extensions;
    using ChuckDeviceController.Net.Utilities;
    using ChuckDeviceController.Protos;

    // TODO: If webhooks are changed via UI, send from Configurator -> Communicator, alternatively request them every 5 minutes or something
    public class WebhookEndpoint : IWebhookEndpoint
    {
        public bool Enabled => true;

        public string Url => "https://webhook.site/aaa1ba26-1d7c-4774-982d-696ca6dfecd3";

        public double Delay => 5.0;

        public IEnumerable<WebhookPayloadType> AllowedTypes => new List<WebhookPayloadType>
        {
            WebhookPayloadType.Pokemon,
            WebhookPayloadType.Pokestop,
            WebhookPayloadType.Lure,
            WebhookPayloadType.Invasion,
            WebhookPayloadType.Quest,
            WebhookPayloadType.AlternativeQuest,
            WebhookPayloadType.Gym,
            WebhookPayloadType.GymInfo,
            WebhookPayloadType.Egg,
            WebhookPayloadType.Raid,
            WebhookPayloadType.Weather,
            WebhookPayloadType.Account,
        };
    }

    public class WebhookRelayService : IWebhookRelayService
    {
        #region Constants

        public const ushort WebhookRelayIntervalS = 5;
        public const ushort MaximumRetryCount = 3; // TODO: Make 'MaximumRetryCount' configurable

        #endregion

        #region Variables

        private readonly ILogger<IWebhookRelayService> _logger;
        private readonly IGrpcClientService _grpcClientService;
        private readonly List<IWebhookEndpoint> _webhookEndpoints = new();
        private readonly Timer _timer;
        private readonly ushort _timeout = 30; // TODO: Make 'timeout' configurable

        private readonly Dictionary<string, Pokemon> _pokemonEvents = new();
        private readonly Dictionary<string, Pokestop> _pokestopEvents = new();
        private readonly Dictionary<string, Pokestop> _lureEvents = new();
        private readonly Dictionary<string, (Pokestop, Incident)> _invasionEvents = new();
        private readonly Dictionary<string, Pokestop> _questEvents = new();
        private readonly Dictionary<string, Pokestop> _alternativeQuestEvents = new();
        private readonly Dictionary<string, Gym> _gymEvents = new();
        private readonly Dictionary<string, Gym> _gymInfoEvents = new();
        private readonly Dictionary<string, Gym> _eggEvents = new();
        private readonly Dictionary<string, Gym> _raidEvents = new();
        private readonly Dictionary<long, Weather> _weatherEvents = new();
        private readonly Dictionary<string, Account> _accountEvents = new();

        private readonly object _webhookLock = new();
        private readonly object _pokemonLock = new();
        private readonly object _pokestopsLock = new();
        private readonly object _luresLock = new();
        private readonly object _invasionsLock = new();
        private readonly object _questsLock = new();
        private readonly object _alternativeQuestsLock = new();
        private readonly object _gymsLock = new();
        private readonly object _gymInfoLock = new();
        private readonly object _eggsLock = new();
        private readonly object _raidsLock = new();
        private readonly object _weatherLock = new();
        private readonly object _accountsLock = new();

        #endregion

        #region Properties

        /// <summary>
        /// Gets a value determining whether the webhook relay service is running.
        /// </summary>
        public bool IsRunning => _timer.Enabled;

        /// <summary>
        /// Gets the webhook endpoints to relay entity data to.
        /// </summary>
        public IEnumerable<IWebhookEndpoint> WebhookEndpoints => _webhookEndpoints;

        #endregion

        #region Constructor

        public WebhookRelayService(
            ILogger<IWebhookRelayService> logger,
            IGrpcClientService grpcClientService)
        {
            _logger = logger;
            _grpcClientService = grpcClientService;
            // TODO: Request webhook endpoints
            _webhookEndpoints = new List<IWebhookEndpoint>
            {
                new WebhookEndpoint(),
            };

            _timer = new Timer(WebhookRelayIntervalS * 1000);
            _timer.Elapsed += async (sender, e) => await CheckWebhooksAsync();
            Start();
        }

        #endregion

        #region Public Methods

        public void Start()
        {
            if (!_timer.Enabled)
            {
                _timer.Start();
            }
        }

        public void Stop()
        {
            if (_timer.Enabled)
            {
                _timer.Stop();
            }
        }

        public void Reload()
        {
            lock (_webhookLock)
            {
                // TODO: Request fresh webhooks from Configurator
            }
        }

        public void Enqueue(WebhookPayloadType webhookType, string json)
        {
            if (!IsRunning)
            {
                _logger.LogWarning($"Webhook service is not running, unable to enqueue payload.");
                return;
            }

            if (_webhookEndpoints.Count == 0)
            {
                _logger.LogError($"No webhooks configured! Skipping webhook payload...");
                return;
            }

            switch (webhookType)
            {
                case WebhookPayloadType.Pokemon:
                    var pokemon = json.FromJson<Pokemon>();
                    if (pokemon == null)
                    {
                        _logger.LogError($"Failed to deserialize Pokemon webhook payload");
                        return;
                    }
                    lock (_pokemonLock)
                    {
                        _pokemonEvents[pokemon.Id] = pokemon;
                    }
                    break;
                case WebhookPayloadType.Pokestop:
                    var pokestop = json.FromJson<Pokestop>();
                    if (pokestop == null)
                    {
                        _logger.LogError($"Failed to deserialize Pokestop webhook payload");
                        return;
                    }
                    lock (_pokestopsLock)
                    {
                        _pokestopEvents[pokestop.Id] = pokestop;
                    }
                    break;
                case WebhookPayloadType.Lure:
                    var lure = json.FromJson<Pokestop>();
                    if (lure == null)
                    {
                        _logger.LogError($"Failed to deserialize Lure webhook payload");
                        return;
                    }
                    lock (_luresLock)
                    {
                        _lureEvents[lure.Id] = lure;
                    }
                    break;
                case WebhookPayloadType.Invasion:
                    // TODO: Deserialize tuple (Pokestop, Incident)?
                    var invasion = json.FromJson<Incident>();
                    var invasionPokestop = new Pokestop();
                    if (invasion == null)
                    {
                        _logger.LogError($"Failed to deserialize Invasion webhook payload");
                        return;
                    }
                    lock (_invasionsLock)
                    {
                        _invasionEvents[invasion.Id] = (invasionPokestop, invasion);
                    }
                    break;
                case WebhookPayloadType.Quest:
                    var quest = json.FromJson<Pokestop>();
                    if (quest == null)
                    {
                        _logger.LogError($"Failed to deserialize Quest webhook payload");
                        return;
                    }
                    lock (_questsLock)
                    {
                        _questEvents[quest.Id] = quest;
                    }
                    break;
                case WebhookPayloadType.AlternativeQuest:
                    var altQuest = json.FromJson<Pokestop>();
                    if (altQuest == null)
                    {
                        _logger.LogError($"Failed to deserialize Alternative Quest webhook payload");
                        return;
                    }
                    lock (_alternativeQuestsLock)
                    {
                        _alternativeQuestEvents[altQuest.Id] = altQuest;
                    }
                    break;
                case WebhookPayloadType.Gym:
                    var gym = json.FromJson<Gym>();
                    if (gym == null)
                    {
                        _logger.LogError($"Failed to deserialize Gym webhook payload");
                        return;
                    }
                    lock (_gymsLock)
                    {
                        _gymEvents[gym.Id] = gym;
                    }
                    break;
                case WebhookPayloadType.GymInfo:
                    var gymInfo = json.FromJson<Gym>();
                    if (gymInfo == null)
                    {
                        _logger.LogError($"Failed to deserialize GymInfo webhook payload");
                        return;
                    }
                    lock (_gymInfoLock)
                    {
                        _gymInfoEvents[gymInfo.Id] = gymInfo;
                    }
                    break;
                case WebhookPayloadType.Egg:
                    var egg = json.FromJson<Gym>();
                    if (egg == null)
                    {
                        _logger.LogError($"Failed to deserialize Egg webhook payload");
                        return;
                    }
                    lock (_eggsLock)
                    {
                        _eggEvents[egg.Id] = egg;
                    }
                    break;
                case WebhookPayloadType.Raid:
                    var raid = json.FromJson<Gym>();
                    if (raid == null)
                    {
                        _logger.LogError($"Failed to deserialize Raid webhook payload");
                        return;
                    }
                    lock (_raidsLock)
                    {
                        _raidEvents[raid.Id] = raid;
                    }
                    break;
                case WebhookPayloadType.Weather:
                    var weather = json.FromJson<Weather>();
                    if (weather == null)
                    {
                        _logger.LogError($"Failed to deserialize Weather webhook payload");
                        return;
                    }
                    lock (_weatherLock)
                    {
                        _weatherEvents[weather.Id] = weather;
                    }
                    break;
                case WebhookPayloadType.Account:
                    var account = json.FromJson<Account>();
                    if (account == null)
                    {
                        _logger.LogError($"Failed to deserialize Account webhook payload");
                        return;
                    }
                    lock (_accountsLock)
                    {
                        _accountEvents[account.Username] = account;
                    }
                    break;
            }
        }

        #endregion

        #region Private Methods

        private async Task CheckWebhooksAsync()
        {
            Dictionary<string, Pokemon> pokemonEvents = new();
            Dictionary<string, Pokestop> pokestopEvents = new();
            Dictionary<string, Pokestop> lureEvents = new();
            Dictionary<string, (Pokestop, Incident)> invasionEvents = new();
            Dictionary<string, Pokestop> questEvents = new();
            Dictionary<string, Pokestop> alternativeQuestEvents = new();
            Dictionary<string, Gym> gymEvents = new();
            Dictionary<string, Gym> gymInfoEvents = new();
            Dictionary<string, Gym> eggEvents = new();
            Dictionary<string, Gym> raidEvents = new();
            Dictionary<long, Weather> weatherEvents = new();
            Dictionary<string, Account> accountEvents = new();

            #region Build Events List

            lock (_pokemonLock)
            {
                if (_pokemonEvents.Count > 0)
                {
                    pokemonEvents = new(_pokemonEvents);
                    _pokemonEvents.Clear();
                }
            }
            lock (_pokestopsLock)
            {
                if (_pokestopEvents.Count > 0)
                {
                    pokestopEvents = new(_pokestopEvents);
                    _pokestopEvents.Clear();
                }
            }
            lock (_luresLock)
            {
                if (_lureEvents.Count > 0)
                {
                    lureEvents = new(_lureEvents);
                    _lureEvents.Clear();
                }
            }
            lock (_invasionsLock)
            {
                if (_invasionEvents.Count > 0)
                {
                    invasionEvents = new(_invasionEvents);
                    _invasionEvents.Clear();
                }
            }
            lock (_questsLock)
            {
                if (_questEvents.Count > 0)
                {
                    questEvents = new(_questEvents);
                    _questEvents.Clear();
                }
            }
            lock (_alternativeQuestsLock)
            {
                if (_alternativeQuestEvents.Count > 0)
                {
                    alternativeQuestEvents = new(_alternativeQuestEvents);
                    _alternativeQuestEvents.Clear();
                }
            }
            lock (_gymsLock)
            {
                if (_gymEvents.Count > 0)
                {
                    gymEvents = new(_gymEvents);
                    _gymEvents.Clear();
                }
            }
            lock (_gymInfoLock)
            {
                if (_gymInfoEvents.Count > 0)
                {
                    gymInfoEvents = new(_gymInfoEvents);
                    _gymInfoEvents.Clear();
                }
            }
            lock (_eggsLock)
            {
                if (_eggEvents.Count > 0)
                {
                    eggEvents = new(_eggEvents);
                    _eggEvents.Clear();
                }
            }
            lock (_raidsLock)
            {
                if (_raidEvents.Count > 0)
                {
                    raidEvents = new(_raidEvents);
                    _raidEvents.Clear();
                }
            }
            lock (_weatherLock)
            {
                if (_weatherEvents.Count > 0)
                {
                    weatherEvents = new(_weatherEvents);
                    _weatherEvents.Clear();
                }
            }
            lock (_accountsLock)
            {
                if (_accountEvents.Count > 0)
                {
                    accountEvents = new(_accountEvents);
                    _accountEvents.Clear();
                }
            }

            #endregion

            foreach (var endpoint in _webhookEndpoints)
            {
                var events = new List<dynamic>();

                // TODO: Check if entities are within geofence or if geofence is not set
                if (pokemonEvents.Count > 0 && endpoint.AllowedTypes.Contains(WebhookPayloadType.Pokemon))
                {
                    foreach (var (_, pokemon) in pokemonEvents)
                    {
                        events.Add(pokemon.GetWebhookData(WebhookHeaders.Pokemon));
                    }
                }
                if (pokestopEvents.Count > 0 && endpoint.AllowedTypes.Contains(WebhookPayloadType.Pokestop))
                {
                    foreach (var (_, pokestop) in pokestopEvents)
                    {
                        events.Add(pokestop.GetWebhookData(WebhookHeaders.Pokestop));
                    }
                }
                if (lureEvents.Count > 0 && endpoint.AllowedTypes.Contains(WebhookPayloadType.Lure))
                {
                    foreach (var (_, lure) in lureEvents)
                    {
                        events.Add(lure.GetWebhookData(WebhookHeaders.Lure));
                    }
                }
                if (invasionEvents.Count > 0 && endpoint.AllowedTypes.Contains(WebhookPayloadType.Invasion))
                {
                    foreach (var (_, (pokestop, invasion)) in invasionEvents)
                    {
                        events.Add(invasion.GetWebhookData(WebhookHeaders.Invasion, pokestop));
                    }
                }
                if (questEvents.Count > 0 && endpoint.AllowedTypes.Contains(WebhookPayloadType.Quest))
                {
                    foreach (var (_, quest) in questEvents)
                    {
                        events.Add(quest.GetWebhookData(WebhookHeaders.Quest));
                    }
                }
                if (alternativeQuestEvents.Count > 0 && endpoint.AllowedTypes.Contains(WebhookPayloadType.AlternativeQuest))
                {
                    foreach (var (_, alternativeQuest) in alternativeQuestEvents)
                    {
                        events.Add(alternativeQuest.GetWebhookData(WebhookHeaders.AlternativeQuest));
                    }
                }
                if (gymEvents.Count > 0 && endpoint.AllowedTypes.Contains(WebhookPayloadType.Gym))
                {
                    foreach (var (_, gym) in gymEvents)
                    {
                        events.Add(gym.GetWebhookData(WebhookHeaders.Gym));
                    }
                }
                if (gymInfoEvents.Count > 0 && endpoint.AllowedTypes.Contains(WebhookPayloadType.GymInfo))
                {
                    foreach (var (_, gymInfo) in gymInfoEvents)
                    {
                        events.Add(gymInfo.GetWebhookData("gym-info"));
                    }
                }
                if (eggEvents.Count > 0 && endpoint.AllowedTypes.Contains(WebhookPayloadType.Egg))
                {
                    foreach (var (_, egg) in eggEvents)
                    {
                        events.Add(egg.GetWebhookData(WebhookHeaders.Egg));
                    }
                }
                if (raidEvents.Count > 0 && endpoint.AllowedTypes.Contains(WebhookPayloadType.Raid))
                {
                    foreach (var (_, raid) in raidEvents)
                    {
                        events.Add(raid.GetWebhookData(WebhookHeaders.Raid));
                    }
                }
                if (weatherEvents.Count > 0 && endpoint.AllowedTypes.Contains(WebhookPayloadType.Weather))
                {
                    foreach (var (_, weather) in weatherEvents)
                    {
                        events.Add(weather.GetWebhookData(WebhookHeaders.Weather));
                    }
                }
                if (accountEvents.Count > 0 && endpoint.AllowedTypes.Contains(WebhookPayloadType.Account))
                {
                    foreach (var (_, account) in accountEvents)
                    {
                        events.Add(account.GetWebhookData(WebhookHeaders.Account));
                    }
                }

                if (events.Count > 0)
                {
                    await SendWebhookEventsAsync(endpoint.Url, events);
                    Thread.Sleep(Convert.ToInt32(endpoint.Delay * 1000));
                }
            }
        }

        private async Task SendWebhookEventsAsync(string url, List<dynamic> payloads, ushort retryCount = 0)
        {
            if ((payloads?.Count ?? 0) == 0)
                return;

            var json = payloads.ToJson();
            // Send webhook payloads to endpoint with retry count in mind
            var result = await NetUtils.PostAsync(url, json, _timeout);
            if (!string.IsNullOrEmpty(result))
            {
                _logger.LogError($"Webhook endpoint post returned non empty response: {result} for endpoint: '{url}'");
                return;
            }
            _logger.LogInformation($"Sent {payloads!.Count:N0} webhook events to {url} with result: {result}");
        }

        private async Task RequestWebhookEndpointsAsync()
        {
            var response = await _grpcClientService.GetWebhookEndpointsAsync();
            if (response.Status != WebhookEndpointStatus.Ok)
            {
                _logger.LogError($"Failed to retrieve webhook endpoints!");
                return;
            }

            var json = response.Payload;
            var webhooks = json.FromJson<List<Webhook>>();
            // TODO: Set webhook endpoints
        }

        #endregion
    }
}