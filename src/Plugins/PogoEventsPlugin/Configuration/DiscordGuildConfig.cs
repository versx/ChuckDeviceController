namespace PogoEventsPlugin.Configuration;

public class DiscordGuildConfig
{
    public string? Mention { get; set; }

    public List<ulong> UserIds { get; set; } = new();

    public ulong EventsChannelId { get; set; }

    public ulong EventsCategoryChannelId { get; set; }

    public bool DeletePreviousEvents { get; set; } = false;

    public string ChannelNameFormat { get; set; } = "{0}-{1} {2}";
}