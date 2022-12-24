namespace PogoEventsPlugin.Models;

public interface IEventRaidItem : IEventItem
{
    uint? Form { get; }

    uint? TempEvolutionId { get; }
}