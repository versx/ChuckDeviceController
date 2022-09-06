namespace PogoEventsPlugin.Services
{
    using ChuckDeviceController.Extensions.Json;
    using ChuckDeviceController.Net.Utilities;

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

    // TODO: Convert to HostedService?
    public class PokemonEventDataService : IPokemonEventDataService
    {
        private const string EventsEndpoint = "https://raw.githubusercontent.com/ccev/pogoinfo/v2/active/events.json";
        //private const string EventsEndpoint = "https://github.com/WatWowMap/event-info";

        private static ILogger<IPokemonEventDataService> _logger =
            new Logger<IPokemonEventDataService>(LoggerFactory.Create(x => x.AddConsole()));
        private static List<ActiveEvent> _activeEvents = new();

        public IReadOnlyList<IActiveEvent> ActiveEvents => _activeEvents;

        public PokemonEventDataService()
        {
            FetchActiveEventsAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        }

        private async Task FetchActiveEventsAsync()
        {
            var data = await NetUtils.GetAsync(EventsEndpoint);
            if (string.IsNullOrEmpty(data))
            {
                // TODO: Failed to fetch active events
                return;
            }
            var events = data.FromJson<List<ActiveEvent>>();
            if (events == null)
            {
                // TODO: Failed to deserialize fetched active events
                return;
            }

            _activeEvents = events;
        }
    }
}