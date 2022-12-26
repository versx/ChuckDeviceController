namespace PogoEventsPlugin.Services.Discord.Commands;

using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

using ChuckDeviceController.Plugin;

using Extensions;
using Models;

public class SlashCommands : ApplicationCommandModule
{
    private readonly IPokemonEventDataService _eventDataService;
    private readonly ILocalizationHost _localeHost;

    public SlashCommands(
        IPokemonEventDataService eventDataService,
        ILocalizationHost localeHost)
    {
        _eventDataService = eventDataService;
        _localeHost = localeHost;
    }

    [SlashCommand("test", "A test slash command")]
    public async Task TestCommand(InteractionContext ctx)
    {
        var response = new DiscordInteractionResponseBuilder()
            .WithTitle("Test")
            .WithContent("This is a test")
            .AddComponents(new[]
            {
                new DiscordButtonComponent(ButtonStyle.Primary, "1_top", "Test"),
            });
        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, response);
    }

    [SlashCommand("events", "Post all Pokemon events to a specified channel.")]
    public async Task Events(InteractionContext ctx,
        [Option("channel", "Channel to post events")] DiscordChannel? channel = null,
        [Choice("All Events", 0)]
        [Choice("Active Events", 1)]
        [Option("type", "Whether to post all events or only active events")] long type = 1)
    {
        var events = _eventDataService.ActiveEvents
            .Filter(active: type == 1, sorted: true)
            .ToList();
        var embeds = events
            .Select(CreateActiveEventEmbed)
            .ToList();
        embeds.Add(CreateActiveEventQuestsEmbed(_eventDataService.ActiveQuests));

        if (channel != null)
        {
            foreach (var embed in embeds)
            {
                await channel.SendMessageAsync(embed);
                await Task.Delay(200);
            }
        }
        else
        {
            var response = new DiscordInteractionResponseBuilder()
                .AddEmbeds(embeds);
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, response);
        }
    }

    private DiscordEmbed CreateActiveEventEmbed(IActiveEvent @event)
    {
        var availableRaids = _eventDataService.ActiveRaids.Keys.Select(level => $"Level {level}: " + string.Join(", ", _eventDataService.ActiveRaids[level].Select(id => _localeHost.GetPokemonName(id.Id))));
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
        var quests = @event.HasQuests
            ? _eventDataService.ActiveQuests[QuestsType.Event]
            : _eventDataService.ActiveQuests[QuestsType.Quests];
        var eventQuests = string.Join("\n", quests.Select(quest => $"**{quest.Task}:**\n- {string.Join("\n- ", ParseQuestRewards(quest))}"));
        eventQuests = eventQuests[..Math.Min(1024, eventQuests.Length)];
        embedBuilder.AddField("Event Quests", eventQuests);

        var embed = embedBuilder.Build();
        return embed;
    }

    private DiscordEmbed CreateActiveEventQuestsEmbed(IReadOnlyDictionary<QuestsType, IReadOnlyList<IEventQuestItem>> eventQuests)
    {
        var embedBuilder = new DiscordEmbedBuilder
        {
            Author = new DiscordEmbedBuilder.EmbedAuthor
            {
                Name = Strings.DiscordBotName,
            },
            Title = $"Quests Available",
            Color = DiscordColor.Blurple,
            Footer = new DiscordEmbedBuilder.EmbedFooter
            {
                Text = DateTime.Now.ToLongDateString() + " " + DateTime.Now.ToLongTimeString(),
                IconUrl = Strings.PokemonGoIconUrl,
            },
        };

        foreach (var (questType, quests) in eventQuests)
        {
            var data = string.Join("\n", quests.Select(quest => $"**{quest.Task}:**\n- {string.Join("\n- ", ParseQuestRewards(quest))}"));
            embedBuilder.AddField(questType.ToString(), data[..Math.Min(1024, data.Length)]);
        }
        if (embedBuilder.Fields.Count > 25)
        {
            embedBuilder.RemoveFieldRange(25, embedBuilder.Fields.Count);
        }

        var embed = embedBuilder.Build();
        return embed;
    }

    private IEnumerable<string> ParseQuestRewards(IEventQuestItem eventQuest)
    {
        var result = new List<string>();
        foreach (var reward in eventQuest.Rewards)
        {
            switch (reward.Type)
            {
                case "pokemon":
                    result.Add(_localeHost.GetPokemonName(reward.Reward!.Id));
                    break;
                case "energy":
                    result.Add($"{reward.Amount:N0} Mega Energy");
                    break;
                case "item":
                    result.Add($"{reward.Amount:N0} {_localeHost.GetItem(reward.Id ?? 0)}");
                    break;
                case "stardust":
                    result.Add($"{reward.Amount:N0} Stardust");
                    break;
            }
        }
        return result;
    }

    //[SlashCommand("ban", "Bans a user")]
    //public async Task Ban(InteractionContext ctx,
    //    [Option("user", "User to ban")] DiscordUser user,
    //    [Choice("None", 0)]
    //    [Choice("1 Day", 1)]
    //    [Choice("1 Week", 7)]
    //    [Option("deletedays", "Number of days of message history to delete")] long deleteDays = 0)
    //{
    //    //await ctx.Guild.BanMemberAsync(user.Id, (int)deleteDays);
    //    var response = new DiscordInteractionResponseBuilder()
    //        .WithTitle("Banned User")
    //        .WithContent($"Banned {user.Username} and deleted {deleteDays} days worth of messages.");
    //    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, response);
    //}

    //[ContextMenu(ApplicationCommandType.UserContextMenu, "User Menu")] //MessageContextMenu
    //public async Task UserMenu(ContextMenuContext ctx)
    //{
    //    //var builder = new DiscordMessageBuilder();
    //    var response = new DiscordInteractionResponseBuilder()
    //        .WithTitle("MenuContext")
    //        .WithContent("User menu");
    //    await ctx.CreateResponseAsync(InteractionResponseType.Modal, response);
    //}
}