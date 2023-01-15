namespace ChuckDeviceCommunicator.Services;

using System.Collections.Concurrent;
using System.Net;
using System.Timers;

using Microsoft.Extensions.Options;

using ChuckDeviceCommunicator.Extensions;
using ChuckDeviceController.Collections;
using ChuckDeviceController.Common;
using ChuckDeviceController.Configuration;
using ChuckDeviceController.Data.Entities;
using ChuckDeviceController.Extensions.Json;
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

            if (pokemonEvents.Any() && endpoint.Types.HasFlag(WebhookType.Pokemon))
            {
                var webhooks = endpoint.ProcessPokemon(pokemonEvents);
                if (webhooks.Any())
                {
                    events.AddRange(webhooks);
                }
            }
            if (pokestopEvents.Any() && endpoint.Types.HasFlag(WebhookType.Pokestops))
            {
                var webhooks = endpoint.ProcessPokestops(pokestopEvents);
                if (webhooks.Any())
                {
                    events.AddRange(webhooks);
                }
            }
            if (lureEvents.Any() && endpoint.Types.HasFlag(WebhookType.Lures))
            {
                var webhooks = endpoint.ProcessLures(lureEvents);
                if (webhooks.Any())
                {
                    events.AddRange(webhooks);
                }
            }
            if (invasionEvents.Any() && endpoint.Types.HasFlag(WebhookType.Invasions))
            {
                var webhooks = endpoint.ProcessInvasions(invasionEvents);
                if (webhooks.Any())
                {
                    events.AddRange(webhooks);
                }
            }
            if (questEvents.Any() && endpoint.Types.HasFlag(WebhookType.Quests))
            {
                var webhooks = endpoint.ProcessQuests(questEvents);
                if (webhooks.Any())
                {
                    events.AddRange(webhooks);
                }
            }
            if (alternativeQuestEvents.Any() && endpoint.Types.HasFlag(WebhookType.AlternativeQuests))
            {
                var webhooks = endpoint.ProcessAltQuests(alternativeQuestEvents);
                if (webhooks.Any())
                {
                    events.AddRange(webhooks);
                }
            }
            if (gymEvents.Any() && endpoint.Types.HasFlag(WebhookType.Gyms))
            {
                var webhooks = endpoint.ProcessGyms(gymEvents);
                if (webhooks.Any())
                {
                    events.AddRange(webhooks);
                }
            }
            if (gymInfoEvents.Any() && endpoint.Types.HasFlag(WebhookType.GymInfo))
            {
                var webhooks = endpoint.ProcessGymInfo(gymInfoEvents);
                if (webhooks.Any())
                {
                    events.AddRange(webhooks);
                }
            }
            if (gymDefenderEvents.Any() && endpoint.Types.HasFlag(WebhookType.GymDefenders))
            {
                var webhooks = endpoint.ProcessGymDefenders(gymDefenderEvents);
                if (webhooks.Any())
                {
                    events.AddRange(webhooks);
                }
            }
            if (gymTrainerEvents.Any() && endpoint.Types.HasFlag(WebhookType.GymTrainers))
            {
                var webhooks = endpoint.ProcessGymTrainers(gymTrainerEvents);
                if (webhooks.Any())
                {
                    events.AddRange(webhooks);
                }
            }
            if (eggEvents.Any() && endpoint.Types.HasFlag(WebhookType.Eggs))
            {
                var webhooks = endpoint.ProcessEggs(eggEvents);
                if (webhooks.Any())
                {
                    events.AddRange(webhooks);
                }
            }
            if (raidEvents.Any() && endpoint.Types.HasFlag(WebhookType.Raids))
            {
                var webhooks = endpoint.ProcessRaids(raidEvents);
                if (webhooks.Any())
                {
                    events.AddRange(webhooks);
                }
            }
            if (weatherEvents.Any() && endpoint.Types.HasFlag(WebhookType.Weather))
            {
                var webhooks = endpoint.ProcessWeather(weatherEvents);
                if (webhooks.Any())
                {
                    events.AddRange(webhooks);
                }
            }
            if (accountEvents.Count > 0 && endpoint.Types.HasFlag(WebhookType.Accounts))
            {
                var webhooks = endpoint.ProcessAccounts(accountEvents);
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
            _logger.LogInformation("Sent {Count:N0} webhook events to {Url}. Total sent this session: {_totalWebhooksSent}", payloads.Count, url, _totalWebhooksSent);
            return;
        }

        _logger.LogError("Webhook endpoint {Url} did not return an 'OK' status code, {StatusCode} with response: {Result}", url, statusCode, result);

        // Try sending again
        if (retryCount >= Options.MaximumRetryCount)
        {
            _logger.LogWarning("{RetryCount}/{MaximumRetryCount} attempts made to send webhook payload to endpoint {Url}, aborting...", retryCount, Options.MaximumRetryCount, url);
            return;
        }

        // Wait 3 seconds before trying again
        Thread.Sleep(Options.FailedRetryDelayS * 1000);
        retryCount++;
        _logger.LogWarning("Retry attempt {RetryCount}/{MaximumRetryCount} to resend webhook payload to endpoint {Url}", retryCount, Options.MaximumRetryCount, url);

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

            _logger.LogInformation("Successfully retrieved {Count:N0} updated webhook endpoints.", webhooks.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error: {Message}", ex.InnerException?.Message ?? ex.Message);
        }
    }

    #endregion
}