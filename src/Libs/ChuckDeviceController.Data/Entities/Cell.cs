namespace ChuckDeviceController.Data.Entities
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Text.Json.Serialization;

    using ChuckDeviceController.Extensions;
    using ChuckDeviceController.Geometry.Extensions;

    [Table("s2cell")]
    public class Cell : BaseEntity, ICoordinateEntity
    {
        #region Properties

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

        #endregion

        public Cell()
        {
        }

        public Cell(ulong cellId)
        {
            var latlng = cellId.ToCoordinate();
            Id = cellId;
            Latitude = latlng.Latitude;
            Longitude = latlng.Longitude;
            Level = 15;
            Updated = DateTime.UtcNow.ToTotalSeconds();
        }
    }
}