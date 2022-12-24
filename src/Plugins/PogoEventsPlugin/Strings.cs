namespace PogoEventsPlugin;

internal static class Strings
{
    // https://github.com/WatWowMap/event-info
    public const string BaseEventsEndpoint = "https://raw.githubusercontent.com/ccev/pogoinfo/v2/";
    public const string EventsEndpoint = BaseEventsEndpoint + "active/events.json";
    public const string RaidsEndpoint = BaseEventsEndpoint + "active/raids.json";
    public const string QuestsEndpoint = BaseEventsEndpoint + "active/quests.json";
    public const string GruntsEndpoint = BaseEventsEndpoint + "active/grunts.json";
    public const string NestsEndpoint = BaseEventsEndpoint + "nests/species-ids.json";

    public const string DiscordBotActivity = "Finding Pokemon Events";
    public const string DiscordBotName = "Pokemon Go Event Watcher";
    public const string NewEventFoundTitle = "**New Event Found**";
    public const string PokemonGoIconUrl = "https://www.creativefreedom.co.uk/wp-content/uploads/2016/07/pokemon1.png";
}