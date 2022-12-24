namespace PogoEventsPlugin.Extensions;

using DSharpPlus;
using DSharpPlus.Entities;

public static class DiscordExtensions
{
    public static async Task<bool> DeleteChannelAsync(this DiscordChannel channel)
    {
        // Only delete voice channels
        if (channel.Type != ChannelType.Voice)
        {
            return false;
        }

        try
        {
            await channel.DeleteAsync("Event has expired");
            return true;
        }
        catch
        {
            return false;
        }
    }
}