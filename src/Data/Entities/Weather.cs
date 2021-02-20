namespace ChuckDeviceController.Data.Entities
{
    using ChuckDeviceController.Data.Interfaces;
    using ChuckDeviceController.Geofence.Models;
    using Google.Common.Geometry;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using WeatherCondition = POGOProtos.Rpc.GameplayWeatherProto.Types.WeatherCondition;

    [Table("weather")]
    public class Weather : BaseEntity, IAggregateRoot, IWebhook
    {
        [
            Column("id"),
            Key,
        ]
        public long Id { get; set; }

        [Column("level")]
        public ushort Level { get; set; }

        [Column("latitude")]
        public double Latitude { get; set; }

        [Column("longitude")]
        public double Longitude { get; set; }

        [Column("gameplay_condition")]
        public WeatherCondition GameplayCondition { get; set; }

        [Column("wind_direction")]
        public ushort WindDirection { get; set; }

        [Column("cloud_level")]
        public ushort CloudLevel { get; set; }

        [Column("rain_level")]
        public ushort RainLevel { get; set; }

        [Column("wind_level")]
        public ushort WindLevel { get; set; }

        [Column("snow_level")]
        public ushort SnowLevel { get; set; }

        [Column("fog_level")]
        public ushort FogLevel { get; set; }

        [Column("special_effect_level")]
        public ushort SpecialEffectLevel { get; set; }

        [Column("severity")]
        public ushort? Severity { get; set; }

        [Column("warn_weather")]
        public bool? WarnWeather { get; set; }

        [Column("updated")]
        public ulong Updated { get; set; }

        public dynamic GetWebhookValues(string type)
        {
            S2Cell s2cell = new S2Cell(new S2CellId((ulong)Id));
            MultiPolygon _polygon = new MultiPolygon();
            for (int i = 0; i <= 3; i++)
            {
                S2Point vertex = s2cell.GetVertex(i);
                S2LatLng coord = new S2LatLng(vertex);
                _polygon.Add(new Polygon
                {
                    coord.LatDegrees,
                    coord.LngDegrees,
                });
            }
            return new
            {
                type = "weather",
                message = new
                {
                    s2_cell_id = Id,
                    latitude = Latitude,
                    longitude = Longitude,
                    polygon = _polygon,
                    gameplay_condition = (ushort)GameplayCondition,
                    wind_direction = WindDirection,
                    cloud_level = CloudLevel,
                    rain_level = RainLevel,
                    wind_level = WindLevel,
                    snow_level = SnowLevel,
                    fog_level = FogLevel,
                    special_effect_level = SpecialEffectLevel,
                    severity = Severity,
                    warn_weather = WarnWeather,
                    updated = Updated,
                },
            };
        }
    }
}
