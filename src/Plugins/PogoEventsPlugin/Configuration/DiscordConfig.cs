namespace PogoEventsPlugin.Configuration;

public class DiscordConfig
{
    public bool Enabled { get; set; }

    public string Token { get; set; } = null!;

    public LogLevel LogLevel { get; set; } = LogLevel.Information;

    public Dictionary<ulong, DiscordGuildConfig> Guilds { get; set; } = new();
}