namespace ChuckDeviceController.Data.Entities
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Text.Json.Serialization;

    using ChuckDeviceController.Common.Data.Contracts;
    using ChuckDeviceController.Data.Contracts;
    using ChuckDeviceController.Extensions;
    using ChuckDeviceController.Geometry.Extensions;

    [Table("s2cell")]
    public class Cell : BaseEntity, ICell, ICoordinateEntity
    {
        private const ushort S2CellLevel = 15;

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

        //public virtual ICollection<Gym>? Gyms { get; set; }

        //public virtual ICollection<Pokemon>? Pokemon { get; set; }

        //public virtual ICollection<Pokestop>? Pokestops { get; set; }

        #endregion

        #region Constructor

        public Cell()
        {
        }

        public Cell(ulong cellId)
        {
            var latlng = cellId.ToCoordinate();
            Id = cellId;
            Latitude = latlng.Latitude;
            Longitude = latlng.Longitude;
            Level = S2CellLevel;
            Updated = DateTime.UtcNow.ToTotalSeconds();
        }

        #endregion
    }
}