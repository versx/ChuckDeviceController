namespace PogoEventsPlugin.Models;

public interface IEventItem
{
    uint Id { get; }

    string Template { get; }
}