namespace PogoEventsPlugin.Models
{
    public class EventItem : IEventItem
    {
        public uint Id { get; set; }

        public string Template { get; set; }
    }
}