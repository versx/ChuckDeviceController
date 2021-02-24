namespace ChuckDeviceController.Data.Entities
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    using ChuckDeviceController.Data.Interfaces;

    [Table("spawnpoint")]
    public class Spawnpoint : BaseEntity, IAggregateRoot
    {
        [
            Column("id"),
            Key,
        ]
        public ulong Id { get; set; }

        [Column("lat")]
        public double Latitude { get; set; }

        [Column("lon")]
        public double Longitude { get; set; }

        [Column("despawn_sec")]
        public ushort? DespawnSecond { get; set; }

        [Column("updated")]
        public ulong Updated { get; set; }

        [Column("first_seen_timestamp")]
        public ulong FirstSeenTimestamp { get; set; }
    }
}