namespace PogoEventsPlugin.Services
{
    using Models;

    public interface IPokemonEventDataService
    {
        IReadOnlyList<IActiveEvent> ActiveEvents { get; }
    }
}