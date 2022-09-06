namespace PogoEventsPlugin.Models
{
    public class ActiveEventItem : IActiveEventItem
    {
        public uint Id { get; set; }

        public string Template { get; set; }
    }
}