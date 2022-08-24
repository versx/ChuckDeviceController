namespace TodoPlugin.Data.Entities
{
    using System.ComponentModel;

    public class Todo
    {
        [DisplayName("ID")]
        public uint Id { get; set; }

        [DisplayName("Name")]
        public string? Name { get; set; }

        [DisplayName("Is Complete")]
        public bool IsComplete { get; set; }
    }
}