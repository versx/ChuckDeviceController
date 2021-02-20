namespace ChuckDeviceController.Data.Entities
{
    using ChuckDeviceController.Data.Interfaces;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("s2cell")]
    public class Cell : BaseEntity, IAggregateRoot
    {
        [
            Column("id"),
            Key,
        ]
        public ulong Id { get; set; }

        [Column("level")]
        public ushort Level { get; set; }

        [Column("center_lat")]
        public double Latitude { get; set; }

        [Column("center_lon")]
        public double Longitude { get; set; }

        [Column("updated")]
        public ulong Updated { get; set; }
    }
}