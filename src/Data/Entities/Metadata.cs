namespace ChuckDeviceController.Data.Entities
{
    using ChuckDeviceController.Data.Interfaces;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

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