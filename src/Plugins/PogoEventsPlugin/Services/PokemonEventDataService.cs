namespace PogoEventsPlugin.Services;

using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.Options;

using ChuckDeviceController.Extensions.Json;
using ChuckDeviceController.Net.Utilities;
using ChuckDeviceController.Plugin;

using Configuration;
using Extensions;
using Models;
using Services.Discord;
using Services.Discord.Commands;

/*
 * 'active':
 * - events.json
 * - grunts.json
 * - quests.json
 * - raids.json
 * 'nests':
 * - last-regular-migration
 * - species-ids.json
 */

public enum QuestsType
{
    Event,
    Quests,
    Ar,
    Sponsored,
}

public class PokemonEventDataService : IPokemonEventDataService
{
    #region Variables

    private readonly ILogger<IPokemonEventDataService> _logger;
    private readonly DiscordConfig _config;
    private readonly IDiscordClientService _discordClientService = null!;
    private readonly DiscordClient _discordClient = null!;
    private readonly ILocalizationHost _localeHost;
    private readonly EventChangeWatcher _eventWatcher;

    private static List<ActiveEvent> _activeEvents = new();
    private static Dictionary<ushort, List<EventRaidItem>> _activeRaids = new();
    private static Dictionary<QuestsType, List<EventQuestItem>> _activeQuests = new();
    private static Dictionary<string, List<uint>> _activeNestPokemon = new();
    private static Dictionary<uint, EventGruntItem> _activeGrunts = new();

    #endregion

    #region Properties

    public IReadOnlyList<IActiveEvent> ActiveEvents => _activeEvents;

    public IReadOnlyDictionary<ushort, IReadOnlyList<IEventRaidItem>> ActiveRaids =>
        _activeRaids.ToDictionary(x => x.Key, x => (IReadOnlyList<IEventRaidItem>)x.Value);

    public IReadOnlyDictionary<QuestsType, IReadOnlyList<IEventQuestItem>> ActiveQuests =>
        _activeQuests.ToDictionary(x => x.Key, x => (IReadOnlyList<IEventQuestItem>)x.Value);

    public IReadOnlyDictionary<string, IReadOnlyList<uint>> ActiveNestPokemon =>
        _activeNestPokemon.ToDictionary(x => x.Key, x => (IReadOnlyList<uint>)x.Value);

    public IReadOnlyDictionary<uint, IEventGruntItem> ActiveGrunts =>
        _activeGrunts.ToDictionary(x => x.Key, x => (IEventGruntItem)x.Value);

    #endregion

    #region Constructors

    public PokemonEventDataService(
        ILogger<IPokemonEventDataService> logger,
        IOptions<DiscordConfig> options,
        IDiscordClientService discordClientService,
        ILocalizationHost localeHost)
    {
        _logger = logger;
        _config = options.Value;
        _localeHost = localeHost;

        _eventWatcher = new EventChangeWatcher(Strings.EventsEndpoint);
        _eventWatcher.Changed += OnEventChanged;
        _eventWatcher.Start();

        if (_config.Enabled)
        {
            _discordClientService = discordClientService;
            _discordClient = _discordClientService.CreateClientAsync(connect: false).Result;
            _discordClient.Ready += OnClientReady;

            var services = new ServiceCollection()
                .AddSingleton<IPokemonEventDataService>(this)
                .AddSingleton<ILocalizationHost>(_localeHost)
                .BuildServiceProvider();
            var slash = _discordClient.UseSlashCommands(new() { Services = services });
            slash.RegisterCommands<SlashCommands>();
        }
    }

    #endregion

    #region Public Methods

    public async Task StartAsync()
    {
        if (!_eventWatcher.Enabled)
        {
            _eventWatcher.Start();
        }

        if (_config.Enabled)
        {
            var activity = new DiscordActivity(Strings.DiscordBotActivity, ActivityType.Playing);
            await _discordClient.ConnectAsync(activity, UserStatus.Online);
        }

        await RefreshAsync();
    }

    public async Task StopAsync()
    {
        if (_eventWatcher.Enabled)
        {
            _eventWatcher.Stop();
        }
        await Task.CompletedTask;
    }

    public async Task RefreshAsync()
    {
        await FetchActiveEventsAsync();
        await FetchActiveRaidsAsync();
        await FetchActiveQuestsAsync();
        await FetchActiveNestPokemonAsync();
        await FetchActiveGruntsAsync();
    }

    #endregion

    #region Private Methods

    private async void OnEventChanged(object? sender, EventChangeWatcher.EventChangedEventArgs e)
    {
        _activeEvents = e.Events
            .Cast<ActiveEvent>()
            .ToList();
        if (!_config.Enabled)
            return;

        // Update voice channels
        await CreateChannelsAsync();

        // TODO: Create sort by End/Start extensions
        var events = ActiveEvents.ToList();
        events.Sort((a, b) => DateTime.Parse(a.End).CompareTo(DateTime.Parse(b.End)));

        // Post new events
        foreach (var activeEvent in events)
        {
            var embed = CreateActiveEventEmbed(activeEvent);

            foreach (var (guildId, guildConfig) in _config.Guilds)
            {
                var guild = await _discordClient.GetGuildAsync(guildId);
                if (guild == null)
                {
                    continue;
                }

                var channel = guild.GetChannel(guildConfig.EventsChannelId);
                if (guildConfig.DeletePreviousEvents)
                {
                    var messages = await channel.GetMessagesAsync();
                    foreach (var message in messages)
                    {
                        try
                        {
                            await message.DeleteAsync();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"Error: {ex}");
                        }
                    }
                }

                // Send Discord embed
                var content = string.IsNullOrEmpty(guildConfig.Mention)
                    ? $"<{guildConfig.Mention}>"
                    : null;
                await channel.SendMessageAsync(content, embed);

                // Send Discord DM 
                foreach (var userId in guildConfig.UserIds)
                {
                    var member = await guild.GetMemberAsync(userId, updateCache: true);
                    await member.SendMessageAsync(content, embed);
                }
            }
        }
    }

    #endregion

    #region Fetch Methods

    private async Task FetchActiveEventsAsync()
    {
        _activeEvents = await FetchDataAsync<List<ActiveEvent>>(Strings.EventsEndpoint);
    }

    private async Task FetchActiveRaidsAsync()
    {
        _activeRaids = await FetchDataAsync<Dictionary<ushort, List<EventRaidItem>>>(Strings.RaidsEndpoint);
    }

    private async Task FetchActiveQuestsAsync()
    {
        _activeQuests = await FetchDataAsync<Dictionary<QuestsType, List<EventQuestItem>>>(Strings.QuestsEndpoint);
    }

    private async Task FetchActiveNestPokemonAsync()
    {
        _activeNestPokemon = await FetchDataAsync<Dictionary<string, List<uint>>>(Strings.NestsEndpoint);
    }

    private async Task FetchActiveGruntsAsync()
    {
        _activeGrunts = await FetchDataAsync<Dictionary<uint, EventGruntItem>>(Strings.GruntsEndpoint);
    }

    private async Task<T> FetchDataAsync<T>(string endpointUrl)
    {
        var data = await NetUtils.GetAsync(endpointUrl);
        if (string.IsNullOrEmpty(data))
        {
            // Failed to fetch active events
            _logger.LogError($"Failed to fetch active Pokemon Go events manifest.");
            return default!;
        }
        try
        {
            var events = data.FromJson<T>();
            if (events == null)
            {
                // Failed to deserialize fetched active events
                _logger.LogError($"Failed to deserialize fetched active Pokemon Go events manifest.");
                return default!;
            }

            return events;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error: {ex}");
        }
        return default!;
    }

    #endregion

    #region Discord Methods

    private async Task CreateChannelsAsync()
    {
        var activeEvents = ActiveEvents.Filter(active: true, sorted: true);
        foreach (var (guildId, guildConfig) in _config.Guilds)
        {
            await CreateVoiceChannelsAsync(guildId, guildConfig, activeEvents);
        }
    }

    private async Task CreateVoiceChannelsAsync(ulong guildId, DiscordGuildConfig guildConfig, IEnumerable<IActiveEvent>? activeEvents)
    {
        if (guildId == 0 || guildConfig == null || guildConfig.EventsCategoryChannelId == 0 || activeEvents == null)
        {
            return;
        }

        if (!_discordClient.Guilds.ContainsKey(guildId))
        {
            _logger.LogError($"Failed to get guild with id '{guildId}'");
            return;
        }

        var guild = await _discordClient.GetGuildAsync(guildId, withCounts: true);
        if (!guild.Channels.ContainsKey(guildConfig.EventsCategoryChannelId))
        {
            _logger.LogError($"Failed to get channel category with id '{guildConfig.EventsCategoryChannelId}' from guild '{guildId}'");
            return;
        }

        // Set role permissions for event channel category
        var channelCategory = guild.GetChannel(guildConfig.EventsCategoryChannelId);
        SetChannelPermissions(guild, channelCategory);

        // Loop all active events
        foreach (var @event in activeEvents)
        {
            // Get channel name from event name and ends date
            var channelName = @event.FormatEventName(guildConfig.ChannelNameFormat);

            // Check if channel name matches event name, if not delete channel
            var channel = await CreateVoiceChannelAsync(guild, channelName, channelCategory);
            if (channel == null)
            {
                // Failed to delete channel, continue on to the next
                continue;
            }

            // Attempt to delete any expired event channels
            await DeleteExpiredEventsAsync(channelCategory, guildConfig, activeEvents);
        }
    }

    private async Task<DiscordChannel?> CreateVoiceChannelAsync(DiscordGuild guild, string channelName, DiscordChannel channelCategory)
    {
        // Get channel name from event name and ends date
        //var channelName = FormatEventName(event);
        var channel = guild.Channels.Values.FirstOrDefault(x => string.Compare(x.Name, channelName, true) == 0);
        if (channel != null)
        {
            return channel;
        }

        try
        {
            // Channel does not exist, create voice channel with permissions
            var permissions = await GetDefaultPermissions(guild.Id, _discordClient.CurrentUser.Id);
            channel = await guild.CreateChannelAsync(channelName, ChannelType.Voice, channelCategory, overwrites: permissions);
            _logger.LogInformation($"Event voice channel '{channelName}' created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError($"CreateVoiceChannel: {ex}");
        }

        return channel;
    }

    private async Task DeleteExpiredEventsAsync(DiscordChannel categoryChannel, DiscordGuildConfig guildConfig, IEnumerable<IActiveEvent> activeEvents)
    {
        // Check if channels in category exists in active events, if so keep it, otherwise delete it.
        var activeEventNames = activeEvents.Select(evt => evt.FormatEventName(guildConfig.ChannelNameFormat));
        foreach (var channel in categoryChannel.Children)
        {
            // Check if channel does not exist in formatted active event names
            if (!activeEventNames.Contains(channel.Name))
            {
                // Delete channel since it is a past event
                if (!await channel.DeleteChannelAsync())
                {
                    _logger.LogError($"Failed to delete channel '{channel.Id}'");
                }
            }
        }
    }

    private async void SetChannelPermissions(DiscordGuild guild, DiscordChannel channel)
    {
        var everyoneId = guild.Id;
        var botId = _discordClient.CurrentUser.Id;
        var permissions = await GetDefaultPermissions(everyoneId, botId);
        foreach (var perm in permissions)
        {
            switch (perm.Type)
            {
                case OverwriteType.Member:
                    var member = guild.Members[botId];
                    await channel.AddOverwriteAsync(member, perm.Allowed, perm.Denied);
                    break;
                case OverwriteType.Role:
                    var role = guild.Roles[everyoneId];
                    await channel.AddOverwriteAsync(role, perm.Allowed, perm.Denied);
                    break;
            }
        }
    }

    private async Task<IEnumerable<DiscordOverwriteBuilder>> GetDefaultPermissions(ulong guildId, ulong botId)
    {
        var guild = await _discordClient.GetGuildAsync(guildId, withCounts: true);
        var everyoneRole = guild.EveryoneRole;
        var botUser = await guild.GetMemberAsync(botId, updateCache: true);
        var permissions = new List<DiscordOverwriteBuilder>
        {
            new DiscordOverwriteBuilder(everyoneRole)
                .For(everyoneRole)
                .Allow(Permissions.AccessChannels)
                .Deny(Permissions.UseVoice),
            new DiscordOverwriteBuilder(botUser)
                .For(botUser)
                .Allow(Permissions.AccessChannels |
                       Permissions.UseVoice |
                       Permissions.ManageChannels |
                       Permissions.ManageMessages |
                       Permissions.ManageRoles)
        };
        return permissions;
    }

    #endregion

    #region Discord Client Events

    private Task OnClientReady(DiscordClient sender, ReadyEventArgs e)
    {
        _logger.LogInformation($"Logged in as {sender.CurrentUser.Username}#{sender.CurrentUser.Discriminator} ({sender.CurrentUser.Id})");

        //await CreateChannelsAsync();
        if (!ThreadPool.QueueUserWorkItem(async _ => await CreateChannelsAsync()))
        {
            _logger.LogError($"Failed to queue CreateChannelsAsync with thread pool");
        }

        return Task.CompletedTask;
    }

    #endregion

    #region Helper Methods

    private DiscordEmbed CreateActiveEventEmbed(IActiveEvent @event)
    {
        var availableRaids = ActiveRaids.Keys.Select(level => $"Level {level}: " + string.Join(", ", ActiveRaids[level].Select(id => _localeHost.GetPokemonName(id.Id))));
        var description = $"**Name:** {@event.Name}\n";
        if (@event.Start == null)
        {
            description += $"**Starts:** {@event.Start}\n";
        }
        description += $"**Ends:** {@event.End}\n";
        var embedBuilder = new DiscordEmbedBuilder
        {
            Author = new DiscordEmbedBuilder.EmbedAuthor
            {
                Name = Strings.DiscordBotName,
            },
            Title = Strings.NewEventFoundTitle,
            Description = description,
            Color = DiscordColor.Blurple,
            Footer = new DiscordEmbedBuilder.EmbedFooter
            {
                Text = DateTime.Now.ToLongDateString() + " " + DateTime.Now.ToLongTimeString(),
                IconUrl = Strings.PokemonGoIconUrl,
            },
        };

        var bonuses = @event.Bonuses.Any()
            ? $"- {string.Join("\n- ", @event.Bonuses.Select(bonus => bonus.Text))}"
            : "N/A";
        embedBuilder.AddField("Event Bonuses", bonuses, false);
        var features = @event.Features.Any()
            ? $"- {string.Join("\n- ", @event.Features)}"
            : "N/A";
        embedBuilder.AddField("Event Features", features, false);
        embedBuilder.AddField("Last Nest Migration", "N/A", true);
        var pokemon = @event.Spawns.Any()
            ? string.Join(", ", @event.Spawns.Select(spawn => _localeHost.GetPokemonName(spawn.Id)))
            : "N/A";
        embedBuilder.AddField("Event Pokemon Spawns", pokemon, true);
        var eggs = @event.Eggs.Any()
            ? string.Join(", ", @event.Eggs.Select(egg => _localeHost.GetPokemonName(egg.Id)))
            : "N/A";
        embedBuilder.AddField("Event Hatchable Eggs", eggs, true);
        var raids = availableRaids.Any()
            ? string.Join("\n", availableRaids)
            : "N/A";
        embedBuilder.AddField("Event Raids", raids, false);

        var embed = embedBuilder.Build();
        return embed;
    }

    #endregion
}