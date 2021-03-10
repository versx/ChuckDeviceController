namespace Chuck.Data.Entities
{
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    using Chuck.Data.Interfaces;

    [Table("metadata")]
    public class Metadata : BaseEntity, IAggregateRoot
    {
        [
            Column("key"),
            Key
        ]
        public string Key { get; set; }

        [
            Column("value"),
            DefaultValue(null)
        ]
        public string Value { get; set; }
    }
}