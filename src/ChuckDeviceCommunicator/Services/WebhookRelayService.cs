namespace ChuckDeviceCommunicator.Services;

using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Timers;

using Microsoft.Extensions.Options;

using ChuckDeviceController.Collections;
using ChuckDeviceController.Common;
using ChuckDeviceController.Configuration;
using ChuckDeviceController.Data.Common;
using ChuckDeviceController.Data.Entities;
using ChuckDeviceController.Data.Extensions;
using ChuckDeviceController.Extensions.Json;
using ChuckDeviceController.Geometry;
using ChuckDeviceController.Geometry.Extensions;
using ChuckDeviceController.Net.Utilities;
using ChuckDeviceController.Protos;

// TODO: Refactor as stateless

public class WebhookRelayService : IWebhookRelayService
{
    #region Variables

    private readonly ILogger<IWebhookRelayService> _logger;
    private readonly IGrpcClient<WebhookEndpoint.WebhookEndpointClient, WebhookEndpointRequest, WebhookEndpointResponse> _grpcWebhookClient;
    private readonly SafeCollection<Webhook> _webhookEndpoints = new();
    // Timer used to check received webhooks to process
    private readonly Timer _timer;
    // Timer used to request configured webhook endpoints via configurator
    private readonly Timer _requestTimer;
    private ulong _totalWebhooksSent;

    private readonly ConcurrentDictionary<string, Pokemon> _pokemonEvents = new();
    private readonly ConcurrentDictionary<string, Pokestop> _pokestopEvents = new();
    private readonly ConcurrentDictionary<string, Pokestop> _lureEvents = new();
    private readonly ConcurrentDictionary<string, PokestopWithIncident> _invasionEvents = new();
    private readonly ConcurrentDictionary<string, Pokestop> _questEvents = new();
    private readonly ConcurrentDictionary<string, Pokestop> _alternativeQuestEvents = new();
    private readonly ConcurrentDictionary<string, Gym> _gymEvents = new();
    private readonly ConcurrentDictionary<string, Gym> _gymInfoEvents = new();
    private readonly ConcurrentDictionary<ulong, GymWithDefender> _gymDefenderEvents = new();
    private readonly ConcurrentDictionary<string, GymWithTrainer> _gymTrainerEvents = new();
    private readonly ConcurrentDictionary<string, Gym> _eggEvents = new();
    private readonly ConcurrentDictionary<string, Gym> _raidEvents = new();
    private readonly ConcurrentDictionary<long, Weather> _weatherEvents = new();
    private readonly ConcurrentDictionary<string, Account> _accountEvents = new();

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

    /// <summary>
    /// Gets the configuration options for the service.
    /// </summary>
    public WebhookRelayConfig Options { get; }

    #endregion

    #region Constructor

    public WebhookRelayService(
        ILogger<IWebhookRelayService> logger,
        IGrpcClient<WebhookEndpoint.WebhookEndpointClient, WebhookEndpointRequest, WebhookEndpointResponse> grpcWebhookClient,
        IOptions<WebhookRelayConfig> options)
    {
        _logger = logger;
        _grpcWebhookClient = grpcWebhookClient;
        Options = options.Value;

        _timer = new Timer(Options.ProcessingIntervalS * 1000);
        _timer.Elapsed += async (sender, e) => await CheckWebhooksAsync();

        // TODO: Eventually receive webhook endpoints on-demand via configurator upon add/remove/modify
        _requestTimer = new Timer(Options.EndpointsIntervalS * 1000);
        _requestTimer.Elapsed += async (sender, e) => await SendWebhookEndpointsRequestAsync();

        Task.Run(StartAsync).Wait();
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

        if (string.IsNullOrEmpty(json))
        {
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
                _pokemonEvents.AddOrUpdate(pokemon.Id, pokemon, (key, oldValue) => pokemon);
                break;
            case WebhookPayloadType.Pokestop:
                var pokestop = json.FromJson<Pokestop>();
                if (pokestop == null)
                {
                    _logger.LogError($"Failed to deserialize Pokestop webhook payload");
                    return;
                }
                _pokestopEvents.AddOrUpdate(pokestop.Id, pokestop, (key, oldValue) => pokestop);
                break;
            case WebhookPayloadType.Lure:
                var lure = json.FromJson<Pokestop>();
                if (lure == null)
                {
                    _logger.LogError($"Failed to deserialize Lure webhook payload");
                    return;
                }
                _lureEvents.AddOrUpdate(lure.Id, lure, (key, oldValue) => lure);
                break;
            case WebhookPayloadType.Invasion:
                var pokestopWithIncident = json.FromJson<PokestopWithIncident>();
                if (pokestopWithIncident == null)
                {
                    _logger.LogError($"Failed to deserialize Invasion webhook payload");
                    return;
                }
                _invasionEvents.AddOrUpdate(pokestopWithIncident.Pokestop.Id, pokestopWithIncident, (key, oldValue) => pokestopWithIncident);
                break;
            case WebhookPayloadType.Quest:
                var quest = json.FromJson<Pokestop>();
                if (quest == null)
                {
                    _logger.LogError($"Failed to deserialize Quest webhook payload");
                    return;
                }
                _questEvents.AddOrUpdate(quest.Id, quest, (key, oldValue) => quest);
                break;
            case WebhookPayloadType.AlternativeQuest:
                var altQuest = json.FromJson<Pokestop>();
                if (altQuest == null)
                {
                    _logger.LogError($"Failed to deserialize Alternative Quest webhook payload");
                    return;
                }
                _alternativeQuestEvents.AddOrUpdate(altQuest.Id, altQuest, (key, oldValue) => altQuest);
                break;
            case WebhookPayloadType.Gym:
                var gym = json.FromJson<Gym>();
                if (gym == null)
                {
                    _logger.LogError($"Failed to deserialize Gym webhook payload");
                    return;
                }
                _gymEvents.AddOrUpdate(gym.Id, gym, (key, oldValue) => gym);
                break;
            case WebhookPayloadType.GymInfo:
                var gymInfo = json.FromJson<Gym>();
                if (gymInfo == null)
                {
                    _logger.LogError($"Failed to deserialize GymInfo webhook payload");
                    return;
                }
                _gymInfoEvents.AddOrUpdate(gymInfo.Id, gymInfo, (key, oldValue) => gymInfo);
                break;
            case WebhookPayloadType.GymDefender:
                var gymWithDefender = json.FromJson<GymWithDefender>();
                if (gymWithDefender == null)
                {
                    _logger.LogError($"Failed to deserialize GymDefender webhook payload");
                    return;
                }
                _gymDefenderEvents.AddOrUpdate(gymWithDefender.Defender.Id, gymWithDefender, (key, oldValue) => gymWithDefender);
                break;
            case WebhookPayloadType.GymTrainer:
                var gymWithTrainer = json.FromJson<GymWithTrainer>();
                if (gymWithTrainer == null)
                {
                    _logger.LogError($"Failed to deserialize GymTrainer webhook payload");
                    return;
                }
                _gymTrainerEvents.AddOrUpdate(gymWithTrainer.Trainer.Name, gymWithTrainer, (key, oldValue) => gymWithTrainer);
                break;
            case WebhookPayloadType.Egg:
                var egg = json.FromJson<Gym>();
                if (egg == null)
                {
                    _logger.LogError($"Failed to deserialize Egg webhook payload");
                    return;
                }
                _eggEvents.AddOrUpdate(egg.Id, egg, (key, oldValue) => egg);
                break;
            case WebhookPayloadType.Raid:
                var raid = json.FromJson<Gym>();
                if (raid == null)
                {
                    _logger.LogError($"Failed to deserialize Raid webhook payload");
                    return;
                }
                _raidEvents.AddOrUpdate(raid.Id, raid, (key, oldValue) => raid);
                break;
            case WebhookPayloadType.Weather:
                var weather = json.FromJson<Weather>();
                if (weather == null)
                {
                    _logger.LogError($"Failed to deserialize Weather webhook payload");
                    return;
                }
                _weatherEvents.AddOrUpdate(weather.Id, weather, (key, oldValue) => weather);
                break;
            case WebhookPayloadType.Account:
                var account = json.FromJson<Account>();
                if (account == null)
                {
                    _logger.LogError($"Failed to deserialize Account webhook payload");
                    return;
                }
                _accountEvents.AddOrUpdate(account.Username, account, (key, oldValue) => account);
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

        if (_pokemonEvents.Any())
        {
            pokemonEvents = new(_pokemonEvents.ToArray());
            _pokemonEvents.Clear();
        }
        if (_pokestopEvents.Any())
        {
            pokestopEvents = new(_pokestopEvents.ToArray());
            _pokestopEvents.Clear();
        }
        if (_lureEvents.Any())
        {
            lureEvents = new(_lureEvents.ToArray());
            _lureEvents.Clear();
        }
        if (_invasionEvents.Any())
        {
            invasionEvents = new(_invasionEvents.ToArray());
            _invasionEvents.Clear();
        }
        if (_questEvents.Any())
        {
            questEvents = new(_questEvents.ToArray());
            _questEvents.Clear();
        }
        if (_alternativeQuestEvents.Any())
        {
            alternativeQuestEvents = new(_alternativeQuestEvents);
            _alternativeQuestEvents.Clear();
        }
        if (_gymEvents.Any())
        {
            gymEvents = new(_gymEvents.ToArray());
            _gymEvents.Clear();
        }
        if (_gymInfoEvents.Any())
        {
            gymInfoEvents = new(_gymInfoEvents.ToArray());
            _gymInfoEvents.Clear();
        }
        if (_gymDefenderEvents.Any())
        {
            gymDefenderEvents = new(_gymDefenderEvents.ToArray());
            _gymDefenderEvents.Clear();
        }
        if (_gymTrainerEvents.Any())
        {
            gymTrainerEvents = new(_gymTrainerEvents.ToArray());
            _gymTrainerEvents.Clear();
        }
        if (_eggEvents.Any())
        {
            eggEvents = new(_eggEvents.ToArray());
            _eggEvents.Clear();
        }
        if (_raidEvents.Any())
        {
            raidEvents = new(_raidEvents.ToArray());
            _raidEvents.Clear();
        }
        if (_weatherEvents.Any())
        {
            weatherEvents = new(_weatherEvents.ToArray());
            _weatherEvents.Clear();
        }
        if (_accountEvents.Any())
        {
            accountEvents = new(_accountEvents.ToArray());
            _accountEvents.Clear();
        }

        #endregion

        for (var i = 0; i < _webhookEndpoints.Count; i++)
        {
            var events = new List<dynamic>();
            var endpoint = _webhookEndpoints[i];
            if (!(endpoint?.Enabled ?? false))
                continue;

            if (pokemonEvents.Any() && endpoint.Types.Contains(WebhookType.Pokemon))
            {
                var webhooks = ProcessPokemon(endpoint, pokemonEvents);
                if (webhooks.Any())
                {
                    events.AddRange(webhooks);
                }
            }
            if (pokestopEvents.Any() && endpoint.Types.Contains(WebhookType.Pokestops))
            {
                var webhooks = ProcessPokestops(endpoint, pokestopEvents);
                if (webhooks.Any())
                {
                    events.AddRange(webhooks);
                }
            }
            if (lureEvents.Any() && endpoint.Types.Contains(WebhookType.Lures))
            {
                var webhooks = ProcessLures(endpoint, lureEvents);
                if (webhooks.Any())
                {
                    events.AddRange(webhooks);
                }
            }
            if (invasionEvents.Any() && endpoint.Types.Contains(WebhookType.Invasions))
            {
                var webhooks = ProcessInvasions(endpoint, invasionEvents);
                if (webhooks.Any())
                {
                    events.AddRange(webhooks);
                }
            }
            if (questEvents.Any() && endpoint.Types.Contains(WebhookType.Quests))
            {
                var webhooks = ProcessQuests(endpoint, questEvents);
                if (webhooks.Any())
                {
                    events.AddRange(webhooks);
                }
            }
            if (alternativeQuestEvents.Any() && endpoint.Types.Contains(WebhookType.AlternativeQuests))
            {
                var webhooks = ProcessAltQuests(endpoint, alternativeQuestEvents);
                if (webhooks.Any())
                {
                    events.AddRange(webhooks);
                }
            }
            if (gymEvents.Any() && endpoint.Types.Contains(WebhookType.Gyms))
            {
                var webhooks = ProcessGyms(endpoint, gymEvents);
                if (webhooks.Any())
                {
                    events.AddRange(webhooks);
                }
            }
            if (gymInfoEvents.Any() && endpoint.Types.Contains(WebhookType.GymInfo))
            {
                var webhooks = ProcessGymInfo(endpoint, gymInfoEvents);
                if (webhooks.Any())
                {
                    events.AddRange(webhooks);
                }
            }
            if (gymDefenderEvents.Any() && endpoint.Types.Contains(WebhookType.GymDefenders))
            {
                var webhooks = ProcessGymDefenders(endpoint, gymDefenderEvents);
                if (webhooks.Any())
                {
                    events.AddRange(webhooks);
                }
            }
            if (gymTrainerEvents.Any() && endpoint.Types.Contains(WebhookType.GymTrainers))
            {
                var webhooks = ProcessGymTrainers(endpoint, gymTrainerEvents);
                if (webhooks.Any())
                {
                    events.AddRange(webhooks);
                }
            }
            if (eggEvents.Any() && endpoint.Types.Contains(WebhookType.Eggs))
            {
                var webhooks = ProcessEggs(endpoint, eggEvents);
                if (webhooks.Any())
                {
                    events.AddRange(webhooks);
                }
            }
            if (raidEvents.Any() && endpoint.Types.Contains(WebhookType.Raids))
            {
                var webhooks = ProcessRaids(endpoint, raidEvents);
                if (webhooks.Any())
                {
                    events.AddRange(webhooks);
                }
            }
            if (weatherEvents.Any() && endpoint.Types.Contains(WebhookType.Weather))
            {
                var webhooks = ProcessWeather(endpoint, weatherEvents);
                if (webhooks.Any())
                {
                    events.AddRange(webhooks);
                }
            }
            if (accountEvents.Count > 0 && endpoint.Types.Contains(WebhookType.Accounts))
            {
                var webhooks = ProcessAccounts(endpoint, accountEvents);
                if (webhooks.Any())
                {
                    events.AddRange(webhooks);
                }
            }

            if (events.Any())
            {
                await SendWebhookEventsAsync(endpoint.Url, events);
                Thread.Sleep(Convert.ToInt32(endpoint.Delay * 1000));
            }

            // Wait 5 seconds between webhook endpoints
            Thread.Sleep(5 * 1000);
        }
    }

    private async Task SendWebhookEventsAsync(string url, List<dynamic> payloads, ushort retryCount = 0)
    {
        if (!(payloads?.Any() ?? false))
            return;

        var json = payloads.ToJson();
        if (string.IsNullOrEmpty(json))
        {
            _logger.LogError($"Serialized webhook payload is empty, skipping...");
            return;
        }

        // Send webhook payloads to endpoint
        var (statusCode, result) = await NetUtils.PostAsync(url, json, Options.RequestTimeoutS);
        // If the request failed, attempt it again in 3 seconds
        if (statusCode == HttpStatusCode.OK)
        {
            _totalWebhooksSent += Convert.ToUInt64(payloads.Count);
            _logger.LogInformation($"Sent {payloads!.Count:N0} webhook events to {url}. Total sent this session: {_totalWebhooksSent}");
            return;
        }

        _logger.LogError($"Webhook endpoint {url} did not return an 'OK' status code, {statusCode} with response: {result}");

        // Try sending again
        if (retryCount >= Options.MaximumRetryCount)
        {
            _logger.LogWarning($"{retryCount}/{Options.MaximumRetryCount} attempts made to send webhook payload to endpoint {url}, aborting...");
            return;
        }

        // Wait 3 seconds before trying again
        Thread.Sleep(Options.FailedRetryDelayS * 1000);
        retryCount++;
        _logger.LogWarning($"Retry attempt {retryCount}/{Options.MaximumRetryCount} to resend webhook payload to endpoint {url}");

        await SendWebhookEventsAsync(url, payloads!, retryCount);
    }

    private async Task SendWebhookEndpointsRequestAsync()
    {
        _logger.LogInformation($"Requesting webhook endpoints from configurator...");

        try
        {
            var response = await _grpcWebhookClient.SendAsync(new());
            if (response?.Status != WebhookEndpointStatus.Ok)
            {
                _logger.LogError($"Failed to retrieve webhook endpoints!");
                return;
            }

            var json = response.Payload;
            var webhooks = json.FromJson<List<Webhook>>();
            if (!(webhooks?.Any() ?? false))
            {
                _logger.LogError($"Failed to retrieve webhook endpoints, list was null or empty!");
                return;
            }

            // Set webhook endpoints
            _webhookEndpoints.Clear();
            _webhookEndpoints.AddRange(webhooks);

            _logger.LogInformation($"Successfully retrieved {webhooks.Count:N0} updated webhook endpoints.");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error: {ex}");
        }
    }

    private static bool IsPokemonBlacklisted(uint? pokemonId, uint? formId, uint? costumeId, ushort? genderId, IEnumerable<string>? blacklisted = null)
    {
        if (!(blacklisted?.Any() ?? false))
            return false;

        var sb = new StringBuilder();
        sb.Append($"{pokemonId}");
        if (formId > 0) sb.Append($"_f{formId}");
        if (costumeId > 0) sb.Append($"_c{costumeId}");
        if (genderId > 0) sb.Append($"_g{genderId}");

        var key = sb.ToString();
        var matches = blacklisted.Contains(key);
        return matches;
    }

    #endregion

    #region Processing Methods

    // TODO: Move to WebhookExtensions class

    private static IEnumerable<dynamic> ProcessPokemon(Webhook endpoint, IReadOnlyDictionary<string, Pokemon> pokemonEvents)
    {
        foreach (var (_, pokemon) in pokemonEvents)
        {
            if (endpoint.Geofences?.Any() ?? false)
            {
                if (!GeofenceService.IsPointInPolygon(pokemon.ToCoordinate(), endpoint.GeofenceMultiPolygons))
                    continue;
            }
            if (endpoint.Data?.PokemonIds?.Any() ?? false)
            {
                if (IsPokemonBlacklisted(
                    pokemon.PokemonId,
                    pokemon.Form,
                    pokemon.Costume,
                    pokemon.Gender,
                    endpoint.Data.PokemonIds
                ))
                    continue;
            }
            var data = pokemon.GetWebhookData(WebhookHeaders.Pokemon);
            if (data != null)
            {
                yield return data;
            }
        }
    }

    private static IEnumerable<dynamic> ProcessPokestops(Webhook endpoint, IReadOnlyDictionary<string, Pokestop> pokestopEvents)
    {
        foreach (var (_, pokestop) in pokestopEvents)
        {
            if (endpoint.Geofences?.Any() ?? false)
            {
                if (!GeofenceService.IsPointInPolygon(pokestop.ToCoordinate(), endpoint.GeofenceMultiPolygons))
                    continue;
            }
            if (endpoint.Data?.PokestopIds?.Any() ?? false)
            {
                if (endpoint.Data.PokestopIds.Contains(pokestop.Id))
                    continue;
            }
            var data = pokestop.GetWebhookData(WebhookHeaders.Pokestop);
            if (data != null)
            {
                yield return data;
            }
        }
    }

    private static IEnumerable<dynamic> ProcessLures(Webhook endpoint, IReadOnlyDictionary<string, Pokestop> lureEvents)
    {
        foreach (var (_, lure) in lureEvents)
        {
            if (endpoint.Geofences?.Any() ?? false)
            {
                if (!GeofenceService.IsPointInPolygon(lure.ToCoordinate(), endpoint.GeofenceMultiPolygons))
                    continue;
            }
            if (endpoint.Data?.LureIds?.Any() ?? false)
            {
                if (endpoint.Data.LureIds.Contains(lure.LureId))
                    continue;
            }
            var data = lure.GetWebhookData(WebhookHeaders.Lure);
            if (data != null)
            {
                yield return data;
            }
        }
    }

    private static IEnumerable<dynamic> ProcessInvasions(Webhook endpoint, IReadOnlyDictionary<string, PokestopWithIncident> invasionEvents)
    {
        foreach (var (_, pokestopWithIncident) in invasionEvents)
        {
            if (endpoint.Geofences?.Any() ?? false)
            {
                if (!GeofenceService.IsPointInPolygon(pokestopWithIncident.Pokestop.ToCoordinate(), endpoint.GeofenceMultiPolygons))
                    continue;
            }
            if (endpoint.Data?.InvasionIds?.Any() ?? false)
            {
                if (endpoint.Data.InvasionIds.Contains(pokestopWithIncident.Invasion.Character))
                    continue;
            }
            var data = pokestopWithIncident.Invasion.GetWebhookData(WebhookHeaders.Invasion, pokestopWithIncident.Pokestop);
            if (data != null)
            {
                yield return data;
            }
        }
    }

    private static IEnumerable<dynamic> ProcessQuests(Webhook endpoint, IReadOnlyDictionary<string, Pokestop> questEvents)
    {
        foreach (var (_, quest) in questEvents)
        {
            if (endpoint.Geofences?.Any() ?? false)
            {
                if (!GeofenceService.IsPointInPolygon(quest.ToCoordinate(), endpoint.GeofenceMultiPolygons))
                    continue;
            }
            // TODO: Add quest filtering
            //if (endpoint.Data?.Quests.Any() ?? false)
            //{
            //}
            var data = quest.GetWebhookData(WebhookHeaders.Quest);
            if (data != null)
            {
                yield return data;
            }
        }
    }

    private static IEnumerable<dynamic> ProcessAltQuests(Webhook endpoint, IReadOnlyDictionary<string, Pokestop> alternativeQuestEvents)
    {
        foreach (var (_, alternativeQuest) in alternativeQuestEvents)
        {
            if (endpoint.Geofences?.Any() ?? false)
            {
                if (!GeofenceService.IsPointInPolygon(alternativeQuest.ToCoordinate(), endpoint.GeofenceMultiPolygons))
                    continue;
            }
            //if (endpoint.Data?.Quests.Any() ?? false)
            //{
            //}
            var data = alternativeQuest.GetWebhookData(WebhookHeaders.AlternativeQuest);
            if (data != null)
            {
                yield return data;
            }
        }
    }

    private static IEnumerable<dynamic> ProcessGyms(Webhook endpoint, IReadOnlyDictionary<string, Gym> gymEvents)
    {
        foreach (var (_, gym) in gymEvents)
        {
            if ((endpoint.Geofences?.Count ?? 0) > 0)
            {
                if (!GeofenceService.IsPointInPolygon(gym.ToCoordinate(), endpoint.GeofenceMultiPolygons))
                    continue;
            }
            if (endpoint.Data?.GymTeamIds?.Any() ?? false)
            {
                if (endpoint.Data.GymTeamIds.Contains((ushort)gym.Team))
                    continue;
            }
            var data = gym.GetWebhookData(WebhookHeaders.Gym);
            if (data != null)
            {
                yield return data;
            }
        }
    }

    private static IEnumerable<dynamic> ProcessGymInfo(Webhook endpoint, IReadOnlyDictionary<string, Gym> gymInfoEvents)
    {
        foreach (var (_, gymInfo) in gymInfoEvents)
        {
            if (endpoint.Geofences?.Any() ?? false)
            {
                if (!GeofenceService.IsPointInPolygon(gymInfo.ToCoordinate(), endpoint.GeofenceMultiPolygons))
                    continue;
            }
            if (endpoint.Data?.GymIds?.Any() ?? false)
            {
                if (endpoint.Data.GymIds.Contains(gymInfo.Id))
                    continue;
            }
            var data = gymInfo.GetWebhookData("gym-info");
            if (data != null)
            {
                yield return data;
            }
        }
    }

    private static IEnumerable<dynamic> ProcessGymDefenders(Webhook endpoint, IReadOnlyDictionary<ulong, GymWithDefender> gymDefenderEvents)
    {
        foreach (var (_, gymDefender) in gymDefenderEvents)
        {
            if (endpoint.Geofences?.Any() ?? false)
            {
                if (!GeofenceService.IsPointInPolygon(gymDefender.Gym.ToCoordinate(), endpoint.GeofenceMultiPolygons))
                    continue;
            }
            // TODO: Add gym defenders filtering
            var data = gymDefender.Defender.GetWebhookData("gym-defender", gymDefender.Gym);
            if (data != null)
            {
                yield return data;
            }
        }
    }

    private static IEnumerable<dynamic> ProcessGymTrainers(Webhook endpoint, IReadOnlyDictionary<string, GymWithTrainer> gymTrainerEvents)
    {
        foreach (var (_, gymTrainer) in gymTrainerEvents)
        {
            if (endpoint.Geofences?.Any() ?? false)
            {
                if (!GeofenceService.IsPointInPolygon(gymTrainer.Gym.ToCoordinate(), endpoint.GeofenceMultiPolygons))
                    continue;
            }
            // TODO: Add gym trainers filtering
            var data = gymTrainer.Trainer.GetWebhookData("gym-trainer", gymTrainer.Gym);
            if (data != null)
            {
                yield return data;
            }
        }
    }

    private static IEnumerable<dynamic> ProcessEggs(Webhook endpoint, IReadOnlyDictionary<string, Gym> eggEvents)
    {
        foreach (var (_, egg) in eggEvents)
        {
            if (endpoint.Geofences?.Any() ?? false)
            {
                if (!GeofenceService.IsPointInPolygon(egg.ToCoordinate(), endpoint.GeofenceMultiPolygons))
                    continue;
            }
            if (endpoint.Data?.EggLevels?.Any() ?? false)
            {
                if (endpoint.Data.EggLevels.Contains(egg.RaidLevel ?? 0))
                    continue;
            }
            var data = egg.GetWebhookData(WebhookHeaders.Egg);
            if (data != null)
            {
                yield return data;
            }
        }
    }

    private static IEnumerable<dynamic> ProcessRaids(Webhook endpoint, IReadOnlyDictionary<string, Gym> raidEvents)
    {
        foreach (var (_, raid) in raidEvents)
        {
            if (endpoint.Geofences?.Any() ?? false)
            {
                if (!GeofenceService.IsPointInPolygon(raid.ToCoordinate(), endpoint.GeofenceMultiPolygons))
                    continue;
            }
            if (endpoint.Data?.RaidPokemonIds?.Any() ?? false)
            {
                if (IsPokemonBlacklisted(
                    raid.RaidPokemonId,
                    raid.RaidPokemonForm,
                    raid.RaidPokemonCostume,
                    raid.RaidPokemonGender,
                    endpoint.Data.PokemonIds
                ))
                    continue;
            }
            var data = raid.GetWebhookData(WebhookHeaders.Raid);
            if (data != null)
            {
                yield return data;
            }
        }
    }

    private static IEnumerable<dynamic> ProcessWeather(Webhook endpoint, IReadOnlyDictionary<long, Weather> weatherEvents)
    {
        foreach (var (_, weather) in weatherEvents)
        {
            if (endpoint.Geofences?.Any() ?? false)
            {
                if (!GeofenceService.IsPointInPolygon(weather.ToCoordinate(), endpoint.GeofenceMultiPolygons))
                    continue;
            }
            if (endpoint.Data?.WeatherConditionIds?.Any() ?? false)
            {
                if (endpoint.Data.WeatherConditionIds.Contains((ushort)weather.GameplayCondition))
                    continue;
            }
            var data = weather.GetWebhookData(WebhookHeaders.Weather);
            if (data != null)
            {
                yield return data;
            }
        }
    }

    private static IEnumerable<dynamic> ProcessAccounts(Webhook endpoint, IReadOnlyDictionary<string, Account> accountEvents)
    {
        foreach (var (_, account) in accountEvents)
        {
            var data = account.GetWebhookData(WebhookHeaders.Account);
            if (data != null)
            {
                yield return data;
            }
        }
    }

    #endregion
}