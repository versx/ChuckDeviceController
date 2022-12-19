namespace PogoEventsPlugin.Models;

public class EventBonusItem : IEventBonusItem
{
    public string Text { get; set; } = null!;

    public string Template { get; set; } = null!;
}