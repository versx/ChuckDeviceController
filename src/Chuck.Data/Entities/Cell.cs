namespace Chuck.Data.Entities
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Text.Json.Serialization;

    using Google.Common.Geometry;

    using Chuck.Data.Interfaces;
    using Chuck.Extensions;

    [Table("s2cell")]
    public class Cell : BaseEntity, IAggregateRoot
    {
        [
            Column("id"),
            Key,
            DatabaseGenerated(DatabaseGeneratedOption.None),
            JsonPropertyName("id"),
        ]
        public ulong Id { get; set; }

        [
            Column("level"),
            JsonPropertyName("level"),
        ]
        public ushort Level { get; set; }

        [
            Column("center_lat"),
            JsonPropertyName("center_lat"),
        ]
        public double Latitude { get; set; }

        [
            Column("center_lon"),
            JsonPropertyName("center_lon"),
        ]
        public double Longitude { get; set; }

        [
            Column("updated"),
            JsonPropertyName("updated"),
        ]
        public ulong Updated { get; set; }

        public Cell(ulong cellId)
        {
            var s2cell = new S2Cell(new S2CellId(cellId));
            var center = s2cell.RectBound.Center;
            Id = cellId;
            Latitude = center.LatDegrees;
            Longitude = center.LngDegrees;
            Level = 15;
            Updated = DateTime.UtcNow.ToTotalSeconds();
        }
    }
}