namespace PogoEventsPlugin.Services;

using Models;

public interface IPokemonEventDataService
{
    IReadOnlyList<IActiveEvent> ActiveEvents { get; }

    IReadOnlyDictionary<ushort, IReadOnlyList<IEventRaidItem>> ActiveRaids { get; }

    IReadOnlyDictionary<QuestsType, IReadOnlyList<IEventQuestItem>> ActiveQuests { get; }

    IReadOnlyDictionary<string, IReadOnlyList<uint>> ActiveNestPokemon { get; }

    IReadOnlyDictionary<uint, IEventGruntItem> ActiveGrunts { get; }


    Task StartAsync();

    Task StopAsync();

    Task RefreshAsync();
}