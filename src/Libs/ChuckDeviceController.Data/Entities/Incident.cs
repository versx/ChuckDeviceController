namespace ChuckDeviceController.Data.Entities
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    using POGOProtos.Rpc;

    using ChuckDeviceController.Extensions;

    [Table("incident")]
    public class Incident : BaseEntity
    {
        #region Properties

        [
            Column("id"),
            Key,
            DatabaseGenerated(DatabaseGeneratedOption.None),
        ]
        public string Id { get; set; }

        [Column("pokestop_id")]
        public string PokestopId { get; set; }

        [Column("start")]
        public ulong Start { get; set; }

        [Column("expiration")]
        public ulong Expiration { get; set; }

        [Column("display_type")]
        public uint DisplayType { get; set; }

        [Column("style")]
        public uint Style { get; set; }

        [Column("character")]
        public uint Character { get; set; }

        [Column("updated")]
        public ulong Updated { get; set; }

        [NotMapped]
        public bool HasChanges { get; set; }

        #endregion

        public Incident()
        {
        }

        public Incident(ulong now, string pokestopId, PokestopIncidentDisplayProto pokestopDisplay)
        {
            Id = pokestopDisplay.IncidentId;
            PokestopId = pokestopId;
            Start = Convert.ToUInt64(pokestopDisplay.IncidentStartMs / 1000);
            Expiration = Convert.ToUInt64(pokestopDisplay.IncidentExpirationMs / 1000);
            DisplayType = Convert.ToUInt16(pokestopDisplay.IncidentDisplayType);
            Style = Convert.ToUInt16(pokestopDisplay.CharacterDisplay.Style);
            Character = Convert.ToUInt16(pokestopDisplay.CharacterDisplay.Character);
            Updated = now;
        }
    }
}