namespace PogoEventsPlugin.Services.Discord;

using DSharpPlus;

using Microsoft.Extensions.Options;

using Configuration;

public class DiscordClientService : IDiscordClientService
{
    //private readonly ILogger<IDiscordClientService> _logger;
    private readonly DiscordConfig _config;

    public DiscordClientService(
        //ILogger<IDiscordClientService> logger,
        IOptions<DiscordConfig> options)
    {
        //_logger = logger;
        _config = options.Value;
    }

    public async Task<DiscordClient> CreateClientAsync(bool connect = true)
    {
        var client = new DiscordClient(new DiscordConfiguration
        {
            AlwaysCacheMembers = true,
            AutoReconnect = true,
            GatewayCompressionLevel = GatewayCompressionLevel.Payload,
            Intents = DiscordIntents.DirectMessages |
                DiscordIntents.Guilds |
                DiscordIntents.GuildMembers |
                DiscordIntents.GuildMessages |
                DiscordIntents.GuildPresences |
                DiscordIntents.GuildVoiceStates |
                DiscordIntents.GuildWebhooks,
            MinimumLogLevel = _config.LogLevel,
            ReconnectIndefinitely = true,
            Token = _config.Token,
            TokenType = TokenType.Bot,
            UseRelativeRatelimit = true,
        });

        if (connect)
        {
            await client.ConnectAsync();
        }

        return client;
    }
}