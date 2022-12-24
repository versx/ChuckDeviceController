namespace PogoEventsPlugin.Services;

using DSharpPlus;

public interface IDiscordClientService
{
    Task<DiscordClient> CreateClientAsync(bool connect = true);
}