namespace Chuck.Data.Entities
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Linq;

    using Google.Common.Geometry;
    using POGOProtos.Rpc;
    using WeatherCondition = POGOProtos.Rpc.GameplayWeatherProto.Types.WeatherCondition;

    using Chuck.Data.Interfaces;
    using Chuck.Extensions;

    [Table("weather")]
    public class Weather : BaseEntity, IAggregateRoot, IWebhook
    {
        #region Properties

        [
            Column("id"),
            Key,
            DatabaseGenerated(DatabaseGeneratedOption.None),
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

        #endregion

        public Weather()
        {
        }

        public Weather(ClientWeatherProto proto)
        {
            var now = DateTime.UtcNow.ToTotalSeconds();
            var s2cell = new S2Cell(new S2CellId((ulong)proto.S2CellId));
            var center = s2cell.RectBound.Center;
            var alert = proto.Alerts?.FirstOrDefault();
            Id = proto.S2CellId;
            Level = s2cell.Level;
            Latitude = center.LatDegrees;
            Longitude = center.LngDegrees;
            GameplayCondition = proto.GameplayWeather.GameplayCondition;
            WindDirection = (ushort)proto.DisplayWeather.WindDirection;
            CloudLevel = (ushort)proto.DisplayWeather.CloudLevel;
            RainLevel = (ushort)proto.DisplayWeather.RainLevel;
            WindLevel = (ushort)proto.DisplayWeather.WindLevel;
            SnowLevel = (ushort)proto.DisplayWeather.SnowLevel;
            FogLevel = (ushort)proto.DisplayWeather.FogLevel;
            SpecialEffectLevel = (ushort)proto.DisplayWeather.SpecialEffectLevel;
            Severity = (ushort?)proto.Alerts?.FirstOrDefault()?.Severity ?? null;
            WarnWeather = alert?.WarnWeather;
            Updated = now;
        }

        public bool Update(Weather oldWeather = null)
        {
            var now = DateTime.UtcNow.ToTotalSeconds();
            Updated = now;
            var result = false;
            if (oldWeather == null)
            {
                // WebhookController.Instance.AddWeather(this);
                result = true;
            }
            else if (oldWeather.GameplayCondition != GameplayCondition ||
                oldWeather.WarnWeather != WarnWeather)
            {
                // WebhookController.Instance.AddWeather(this);
                result = true;
            }
            return result;
        }

        public dynamic GetWebhookValues(string type)
        {
            var s2cell = new S2Cell(new S2CellId((ulong)Id));
            var polygon = new List<List<double>>();
            for (var i = 0; i <= 3; i++)
            {
                var vertex = s2cell.GetVertex(i);
                var coord = new S2LatLng(vertex);
                polygon.Add(new List<double> {
                    coord.LatDegrees,
                    coord.LngDegrees
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
                    polygon = polygon,
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
