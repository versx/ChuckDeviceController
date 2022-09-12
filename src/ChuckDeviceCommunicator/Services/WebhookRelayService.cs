namespace ChuckDeviceCommunicator.Services
{
    using System.Timers;

    using Microsoft.Extensions.Options;

    using ChuckDeviceCommunicator.Services.Rpc;
    using ChuckDeviceController.Common;
    using ChuckDeviceController.Common.Data;
    using ChuckDeviceController.Configuration;
    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Data.Extensions;
    using ChuckDeviceController.Extensions.Json;
    using ChuckDeviceController.Geometry;
    using ChuckDeviceController.Geometry.Extensions;
    using ChuckDeviceController.Net.Utilities;
    using ChuckDeviceController.Protos;

    public class WebhookRelayService : IWebhookRelayService
    {
        #region Constants

        public const ushort WebhookRelayIntervalS = 5; // 5 seconds
        public const ushort RequestWebhookIntervalS = 60; // 60 seconds
        public const ushort RequestFailedRetryDelayS = 3; // 3 seconds

        #endregion

        #region Variables

        private readonly ILogger<IWebhookRelayService> _logger;
        private readonly IGrpcClientService _grpcClientService;
        private readonly List<Webhook> _webhookEndpoints = new();
        private readonly Timer _timer;
        private readonly Timer _requestTimer;
        private ulong _totalWebhooksSent;

        private readonly Dictionary<string, Pokemon> _pokemonEvents = new();
        private readonly Dictionary<string, Pokestop> _pokestopEvents = new();
        private readonly Dictionary<string, Pokestop> _lureEvents = new();
        private readonly Dictionary<string, PokestopWithIncident> _invasionEvents = new();
        private readonly Dictionary<string, Pokestop> _questEvents = new();
        private readonly Dictionary<string, Pokestop> _alternativeQuestEvents = new();
        private readonly Dictionary<string, Gym> _gymEvents = new();
        private readonly Dictionary<string, Gym> _gymInfoEvents = new();
        private readonly Dictionary<ulong, GymWithDefender> _gymDefenderEvents = new();
        private readonly Dictionary<string, GymWithTrainer> _gymTrainerEvents = new();
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
        private readonly object _gymDefendersLock = new();
        private readonly object _gymTrainersLock = new();
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
        public IEnumerable<Webhook> WebhookEndpoints => _webhookEndpoints;

        /// <summary>
        /// Gets the total amount of webhooks sent during this session.
        /// </summary>
        public ulong TotalSent => _totalWebhooksSent;

        public WebhookRelayConfig Options { get; }

        #endregion

        #region Constructor

        public WebhookRelayService(
            ILogger<IWebhookRelayService> logger,
            IGrpcClientService grpcClientService,
            IOptions<WebhookRelayConfig> options)
        {
            _logger = logger;
            _grpcClientService = grpcClientService;
            Options = options.Value;

            _timer = new Timer(WebhookRelayIntervalS * 1000);
            _timer.Elapsed += async (sender, e) => await CheckWebhooksAsync();

            // TODO: Eventually receive webhook endpoints on-demand via configurator upon add/remove/modify
            _requestTimer = new Timer(RequestWebhookIntervalS * 1000);
            _requestTimer.Elapsed += async (sender, e) => await SendWebhookEndpointsRequestAsync();

            StartAsync().ConfigureAwait(false)
                        .GetAwaiter()
                        .GetResult();
        }

        #endregion

        #region Public Methods

        public async Task StartAsync()
        {
            _logger.LogInformation($"Starting webhook relay service...");
            if (!_timer.Enabled)
            {
                _timer.Start();
            }

            if (!_requestTimer.Enabled)
            {
                _requestTimer.Start();
            }

            await SendWebhookEndpointsRequestAsync();
        }

        public async Task StopAsync()
        {
            _logger.LogInformation($"Stopping webhook relay service...");
            if (_timer.Enabled)
            {
                _timer.Stop();
            }

            if (_requestTimer.Enabled)
            {
                _requestTimer.Stop();
            }

            await Task.CompletedTask;
        }

        public async Task ReloadAsync()
        {
            _logger.LogInformation($"Reloading webhook relay service...");

            await SendWebhookEndpointsRequestAsync();
        }

        public async Task EnqueueAsync(WebhookPayloadType webhookType, string json)
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
                    var pokestopWithIncident = json.FromJson<PokestopWithIncident>();
                    if (pokestopWithIncident == null)
                    {
                        _logger.LogError($"Failed to deserialize Invasion webhook payload");
                        return;
                    }
                    lock (_invasionsLock)
                    {
                        _invasionEvents[pokestopWithIncident.Pokestop.Id] = pokestopWithIncident;
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
                case WebhookPayloadType.GymDefender:
                    var gymWithDefender = json.FromJson<GymWithDefender>();
                    if (gymWithDefender == null)
                    {
                        _logger.LogError($"Failed to deserialize GymDefender webhook payload");
                        return;
                    }
                    lock (_gymDefendersLock)
                    {
                        _gymDefenderEvents[gymWithDefender.Defender.Id] = gymWithDefender;
                    }
                    break;
                case WebhookPayloadType.GymTrainer:
                    var gymWithTrainer = json.FromJson<GymWithTrainer>();
                    if (gymWithTrainer == null)
                    {
                        _logger.LogError($"Failed to deserialize GymTrainer webhook payload");
                        return;
                    }
                    lock (_gymTrainersLock)
                    {
                        _gymTrainerEvents[gymWithTrainer.Trainer.Name] = gymWithTrainer;
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

            await Task.CompletedTask;
        }

        #endregion

        #region Private Methods

        private async Task CheckWebhooksAsync()
        {
            Dictionary<string, Pokemon> pokemonEvents = new();
            Dictionary<string, Pokestop> pokestopEvents = new();
            Dictionary<string, Pokestop> lureEvents = new();
            Dictionary<string, PokestopWithIncident> invasionEvents = new();
            Dictionary<string, Pokestop> questEvents = new();
            Dictionary<string, Pokestop> alternativeQuestEvents = new();
            Dictionary<string, Gym> gymEvents = new();
            Dictionary<string, Gym> gymInfoEvents = new();
            Dictionary<ulong, GymWithDefender> gymDefenderEvents = new();
            Dictionary<string, GymWithTrainer> gymTrainerEvents = new();
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
            lock (_gymDefendersLock)
            {
                if (_gymDefenderEvents.Count > 0)
                {
                    gymDefenderEvents = new(_gymDefenderEvents);
                    _gymDefenderEvents.Clear();
                }
            }
            lock (_gymTrainersLock)
            {
                if (_gymTrainerEvents.Count > 0)
                {
                    gymTrainerEvents = new(_gymTrainerEvents);
                    _gymTrainerEvents.Clear();
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

            for (var i = 0; i < _webhookEndpoints.Count; i++)
            {
                var events = new List<dynamic>();
                var endpoint = _webhookEndpoints[i];
                if (!endpoint.Enabled)
                    continue;

                if (pokemonEvents.Count > 0 && endpoint.Types.Contains(WebhookType.Pokemon))
                {
                    foreach (var (_, pokemon) in pokemonEvents)
                    {
                        if ((endpoint.Geofences?.Count ?? 0) > 0)
                        {
                            if (!GeofenceService.IsPointInPolygon(pokemon.ToCoordinate(), endpoint.GeofenceMultiPolygons))
                                continue;
                        }
                        events.Add(pokemon.GetWebhookData(WebhookHeaders.Pokemon));
                    }
                }
                if (pokestopEvents.Count > 0 && endpoint.Types.Contains(WebhookType.Pokestops))
                {
                    foreach (var (_, pokestop) in pokestopEvents)
                    {
                        if ((endpoint.Geofences?.Count ?? 0) > 0)
                        {
                            if (!GeofenceService.IsPointInPolygon(pokestop.ToCoordinate(), endpoint.GeofenceMultiPolygons))
                                continue;
                        }
                        events.Add(pokestop.GetWebhookData(WebhookHeaders.Pokestop));
                    }
                }
                if (lureEvents.Count > 0 && endpoint.Types.Contains(WebhookType.Lures))
                {
                    foreach (var (_, lure) in lureEvents)
                    {
                        if ((endpoint.Geofences?.Count ?? 0) > 0)
                        {
                            if (!GeofenceService.IsPointInPolygon(lure.ToCoordinate(), endpoint.GeofenceMultiPolygons))
                                continue;
                        }
                        events.Add(lure.GetWebhookData(WebhookHeaders.Lure));
                    }
                }
                if (invasionEvents.Count > 0 && endpoint.Types.Contains(WebhookType.Invasions))
                {
                    foreach (var (_, pokestopWithIncident) in invasionEvents)
                    {
                        if ((endpoint.Geofences?.Count ?? 0) > 0)
                        {
                            if (!GeofenceService.IsPointInPolygon(pokestopWithIncident.Pokestop.ToCoordinate(), endpoint.GeofenceMultiPolygons))
                                continue;
                        }
                        events.Add(pokestopWithIncident.Invasion.GetWebhookData(WebhookHeaders.Invasion, pokestopWithIncident.Pokestop));
                    }
                }
                if (questEvents.Count > 0 && endpoint.Types.Contains(WebhookType.Quests))
                {
                    foreach (var (_, quest) in questEvents)
                    {
                        if ((endpoint.Geofences?.Count ?? 0) > 0)
                        {
                            if (!GeofenceService.IsPointInPolygon(quest.ToCoordinate(), endpoint.GeofenceMultiPolygons))
                                continue;
                        }
                        events.Add(quest.GetWebhookData(WebhookHeaders.Quest));
                    }
                }
                if (alternativeQuestEvents.Count > 0 && endpoint.Types.Contains(WebhookType.AlternativeQuests))
                {
                    foreach (var (_, alternativeQuest) in alternativeQuestEvents)
                    {
                        if ((endpoint.Geofences?.Count ?? 0) > 0)
                        {
                            if (!GeofenceService.IsPointInPolygon(alternativeQuest.ToCoordinate(), endpoint.GeofenceMultiPolygons))
                                continue;
                        }
                        events.Add(alternativeQuest.GetWebhookData(WebhookHeaders.AlternativeQuest));
                    }
                }
                if (gymEvents.Count > 0 && endpoint.Types.Contains(WebhookType.Gyms))
                {
                    foreach (var (_, gym) in gymEvents)
                    {
                        if ((endpoint.Geofences?.Count ?? 0) > 0)
                        {
                            if (!GeofenceService.IsPointInPolygon(gym.ToCoordinate(), endpoint.GeofenceMultiPolygons))
                                continue;
                        }
                        events.Add(gym.GetWebhookData(WebhookHeaders.Gym));
                    }
                }
                if (gymInfoEvents.Count > 0 && endpoint.Types.Contains(WebhookType.GymInfo))
                {
                    foreach (var (_, gymInfo) in gymInfoEvents)
                    {
                        if ((endpoint.Geofences?.Count ?? 0) > 0)
                        {
                            if (!GeofenceService.IsPointInPolygon(gymInfo.ToCoordinate(), endpoint.GeofenceMultiPolygons))
                                continue;
                        }
                        events.Add(gymInfo.GetWebhookData("gym-info"));
                    }
                }
                if (gymDefenderEvents.Count > 0 && endpoint.Types.Contains(WebhookType.GymDefenders))
                {
                    foreach (var (_, gymDefender) in gymDefenderEvents)
                    {
                        if ((endpoint.Geofences?.Count ?? 0) > 0)
                        {
                            if (!GeofenceService.IsPointInPolygon(gymDefender.Gym.ToCoordinate(), endpoint.GeofenceMultiPolygons))
                                continue;
                        }
                        events.Add(gymDefender.Defender.GetWebhookData("gym-defender", gymDefender.Gym));
                    }
                }
                if (gymTrainerEvents.Count > 0 && endpoint.Types.Contains(WebhookType.GymTrainers))
                {
                    foreach (var (_, gymTrainer) in gymTrainerEvents)
                    {
                        if ((endpoint.Geofences?.Count ?? 0) > 0)
                        {
                            if (!GeofenceService.IsPointInPolygon(gymTrainer.Gym.ToCoordinate(), endpoint.GeofenceMultiPolygons))
                                continue;
                        }
                        events.Add(gymTrainer.Trainer.GetWebhookData("gym-trainer", gymTrainer.Gym));
                    }
                }
                if (eggEvents.Count > 0 && endpoint.Types.Contains(WebhookType.Eggs))
                {
                    foreach (var (_, egg) in eggEvents)
                    {
                        if ((endpoint.Geofences?.Count ?? 0) > 0)
                        {
                            if (!GeofenceService.IsPointInPolygon(egg.ToCoordinate(), endpoint.GeofenceMultiPolygons))
                                continue;
                        }
                        events.Add(egg.GetWebhookData(WebhookHeaders.Egg));
                    }
                }
                if (raidEvents.Count > 0 && endpoint.Types.Contains(WebhookType.Raids))
                {
                    foreach (var (_, raid) in raidEvents)
                    {
                        if ((endpoint.Geofences?.Count ?? 0) > 0)
                        {
                            if (!GeofenceService.IsPointInPolygon(raid.ToCoordinate(), endpoint.GeofenceMultiPolygons))
                                continue;
                        }
                        events.Add(raid.GetWebhookData(WebhookHeaders.Raid));
                    }
                }
                if (weatherEvents.Count > 0 && endpoint.Types.Contains(WebhookType.Weather))
                {
                    foreach (var (_, weather) in weatherEvents)
                    {
                        if ((endpoint.Geofences?.Count ?? 0) > 0)
                        {
                            if (!GeofenceService.IsPointInPolygon(weather.ToCoordinate(), endpoint.GeofenceMultiPolygons))
                                continue;
                        }
                        events.Add(weather.GetWebhookData(WebhookHeaders.Weather));
                    }
                }
                if (accountEvents.Count > 0 && endpoint.Types.Contains(WebhookType.Accounts))
                {
                    foreach (var (_, account) in accountEvents)
                    {
                        events.Add(account.GetWebhookData(WebhookHeaders.Account));
                    }
                }

                if (events.Count > 0)
                {
                    _totalWebhooksSent += Convert.ToUInt64(events.Count);
                    await SendWebhookEventsAsync(endpoint.Url, events);
                    Thread.Sleep(Convert.ToInt32(endpoint.Delay * 1000));
                }

                // Wait 5 seconds between webhook endpoints
                Thread.Sleep(5 * 1000);
            }
        }

        private async Task SendWebhookEventsAsync(string url, List<dynamic> payloads, ushort retryCount = 0)
        {
            if ((payloads?.Count ?? 0) == 0)
                return;

            var json = payloads.ToJson();
            if (string.IsNullOrEmpty(json))
            {
                _logger.LogError($"Serialized webhook payload is empty, skipping...");
                return;
            }

            // Send webhook payloads to endpoint
            var (statusCode, result) = await NetUtils.PostAsync(url, json, Options.RequestTimeout);
            // If the request failed, attempt it again in 3 seconds
            if (statusCode != System.Net.HttpStatusCode.OK)
            {
                _logger.LogError($"Webhook endpoint {url} did not return an 'OK' status code, {statusCode} with response: {result}");

                // Try sending again
                if (retryCount >= Options.MaximumRetryCount)
                {
                    _logger.LogWarning($"{retryCount}/{Options.MaximumRetryCount} attempts made to send webhook payload to endpoint {url}, aborting...");
                    return;
                }

                // Wait 3 seconds before trying again
                Thread.Sleep(RequestFailedRetryDelayS * 1000);
                retryCount++;
                _logger.LogWarning($"Retry attempt {retryCount}/{Options.MaximumRetryCount} to resend webhook payload to endpoint {url}");

                await SendWebhookEventsAsync(url, payloads!, retryCount);
                return;
            }
            _logger.LogInformation($"Sent {payloads!.Count:N0} webhook events to {url}. Total sent this session: {_totalWebhooksSent}");
        }

        private async Task SendWebhookEndpointsRequestAsync()
        {
            _logger.LogInformation($"Requesting webhook endpoints from configurator...");

            try
            {
                var response = await _grpcClientService.GetWebhookEndpointsAsync();
                if (response.Status != WebhookEndpointStatus.Ok)
                {
                    _logger.LogError($"Failed to retrieve webhook endpoints!");
                    return;
                }

                var json = response.Payload;
                var webhooks = json.FromJson<List<Webhook>>();
                if (webhooks == null || webhooks.Count == 0)
                {
                    _logger.LogError($"Failed to retrieve webhook endpoints, list was null or empty!");
                    return;
                }

                // Set webhook endpoints
                lock (_webhookLock)
                {
                    _webhookEndpoints.Clear();
                    _webhookEndpoints.AddRange(webhooks);
                }

                _logger.LogInformation($"Successfully retrieved updated webhook endpoints.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error: {ex}");
            }
        }

        #endregion
    }
}