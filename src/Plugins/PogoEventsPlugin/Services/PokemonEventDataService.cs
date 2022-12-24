namespace PogoEventsPlugin.Services;

using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Options;

using ChuckDeviceController.Extensions;
using ChuckDeviceController.Extensions.Json;
using ChuckDeviceController.Net.Utilities;

using Configuration;
using Models;

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

public class PokemonEventDataService : IPokemonEventDataService
{
    private static ILogger<IPokemonEventDataService> _logger =
        new Logger<IPokemonEventDataService>(LoggerFactory.Create(x => x.AddConsole()));
    private static List<ActiveEvent> _activeEvents = new();
    private readonly DiscordConfig _config;
    private readonly IDiscordClientService _discordClientService = null!;
    private readonly DiscordClient _discordClient = null!;

    public IReadOnlyList<IActiveEvent> ActiveEvents => _activeEvents;

    public PokemonEventDataService(
        IOptions<DiscordConfig> options,
        IDiscordClientService discordClientService)
    {
        _config = options.Value;

        if (_config.Enabled)
        {
            _discordClientService = discordClientService;
            _discordClient = _discordClientService.CreateClientAsync(connect: false).Result;
            _discordClient.Ready += OnClientReady;
            _discordClient.MessageCreated += OnClientMessageCreated;

            var activity = new DiscordActivity("Finding Pokemon Events", ActivityType.Playing);
            Task.Run(async () => await _discordClient.ConnectAsync(activity, UserStatus.Online)).Wait();
        }

        Task.Run(FetchActiveEventsAsync).Wait();
    }

    private async Task FetchActiveEventsAsync()
    {
        var data = await NetUtils.GetAsync(Strings.EventsEndpoint);
        if (string.IsNullOrEmpty(data))
        {
            // Failed to fetch active events
            _logger.LogError($"Failed to fetch active Pokemon Go events manifest.");
            return;
        }
        try
        {
            var events = data.FromJson<List<ActiveEvent>>();
            if (events == null)
            {
                // Failed to deserialize fetched active events
                _logger.LogError($"Failed to deserialize fetched active Pokemon Go events manifest.");
                return;
            }

            _activeEvents = events;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error: {ex}");
        }
    }

    private async Task<IEnumerable<IActiveEvent>?> FilterEventsAsync(bool active = false, bool sorted = false)
    {
        var events = _activeEvents;
        if (events == null)
        {
            await FetchActiveEventsAsync();
        }

        if (!active)
        {
            return events;
        }

        // Now timestamp in seconds
        var now = DateTime.UtcNow.ToTotalSeconds();
        // Filter for only active evnets within todays date
        var activeEvents = events!
            .Where(evt => DateTime.Parse(evt.Start).ToTotalSeconds() <= now && now < DateTime.Parse(evt.End).ToTotalSeconds())
            .ToList();

        // Check if no active events available
        if (!activeEvents.Any())
        {
            // No active events
            //return events;
            return Array.Empty<IActiveEvent>();
        }
        if (sorted)
        {
            // Sort active events by end date
            activeEvents.Sort((a, b) => DateTime.Parse(a.End).CompareTo(DateTime.Parse(b.End)));
        }
        return activeEvents;
    }

    private async Task CreateChannelsAsync()
    {
        var activeEvents = await FilterEventsAsync(active: true, sorted: true);
        foreach (var (guildId, categoryId) in _config.Guilds)
        {
            await CreateVoiceChannelsAsync(guildId, categoryId, activeEvents);
        }
    }

    private async Task CreateVoiceChannelsAsync(ulong guildId, ulong eventCategoryId, IEnumerable<IActiveEvent>? activeEvents)
    {
        if (guildId == 0 || eventCategoryId == 0 || activeEvents == null)
        {
            return;
        }

        if (!_discordClient.Guilds.ContainsKey(guildId))
        {
            _logger.LogError($"Failed to get guild with id '{guildId}'");
            return;
        }

        var guild = await _discordClient.GetGuildAsync(guildId, withCounts: true);
        if (!guild.Channels.ContainsKey(eventCategoryId))
        {
            _logger.LogError($"Failed to get channel category with id '{eventCategoryId}' from guild '{guildId}'");
            return;
        }

        // Set role permissions for event channel category
        var channelCategory = guild.GetChannel(eventCategoryId);
        SetChannelPermissions(guild, channelCategory);

        // Loop all active events
        foreach (var @event in activeEvents)
        {
            // Get channel name from event name and ends date
            var channelName = FormatEventName(@event);

            // Check if channel name matches event name, if not delete channel
            var channel = await CreateVoiceChannelAsync(guild, channelName, channelCategory);
            if (channel == null)
            {
                // Failed to delete channel, continue on to the next
                continue;
            }
            // Attempt to delete any expired event channels
            await DeleteExpiredEventsAsync(channelCategory, activeEvents);
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

        return null;
    }

    private static async Task DeleteChannelAsync(DiscordChannel channel)
    {
        // Only delete voice channels
        if (channel.Type != ChannelType.Voice)
        {
            return;
        }

        try
        {
            await channel.DeleteAsync("Event has expired");
            _logger.LogDebug($"Event voice channel '{channel.Id}' deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to delete channel '{channel.Id}': {ex}");
        }
    }

    private static async Task DeleteExpiredEventsAsync(DiscordChannel categoryChannel, IEnumerable<IActiveEvent> activeEvents)
    {
        // Check if channels in category exists in active events, if so keep it, otherwise delete it.
        var activeEventNames = activeEvents.Select(FormatEventName);
        foreach (var channel in categoryChannel.Children)
        {
            // Check if channel does not exist in formatted active event names
            if (!activeEventNames.Contains(channel.Name))
            {
                // Delete channel if it's not an active event
                await DeleteChannelAsync(channel);
            }
        }
    }

    private static string FormatEventName(IActiveEvent activeEvent)
    {
        var format = "{0}-{1} {2}"; //"{{month}}-{{day}} {{name}}";
        var eventEndDate = //activeEvent.End != null
            DateTime.Parse(activeEvent.End);
            //: "N/A";
        var channelName = string.Format(format, eventEndDate.Month, eventEndDate.Day, activeEvent.Name);
        return channelName;
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

    #region Discord Client Events

    private async Task OnClientReady(DiscordClient sender, ReadyEventArgs e)
    {
        _logger.LogInformation($"Logged in as {sender.CurrentUser.Username}#{sender.CurrentUser.Discriminator} ({sender.CurrentUser.Id})");

        await CreateChannelsAsync();
    }

    private async Task OnClientMessageCreated(DiscordClient sender, MessageCreateEventArgs e)
    {
        // TODO: Add Discord commands
        await Task.CompletedTask;
    }

    #endregion
}