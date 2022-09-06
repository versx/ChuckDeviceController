namespace PogoEventsPlugin.Models
{
    public interface IActiveEventRaidItem : IActiveEventItem
    {
        uint? Form { get; }
    }
}