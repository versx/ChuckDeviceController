namespace PogoEventsPlugin.Services.Discord;

using DSharpPlus;

public interface IDiscordClientService
{
    Task<DiscordClient> CreateClientAsync(bool connect = true);
}