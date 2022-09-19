namespace ChuckDeviceController.Data.Entities
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    using POGOProtos.Rpc;
    using WeatherCondition = POGOProtos.Rpc.GameplayWeatherProto.Types.WeatherCondition;

    using ChuckDeviceController.Common;
    using ChuckDeviceController.Common.Data.Contracts;
    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Data.Contracts;
    using ChuckDeviceController.Extensions;
    using ChuckDeviceController.Geometry.Extensions;
    using ChuckDeviceController.Extensions.Http.Caching;

    [Table("weather")]
    public partial class Weather : BaseEntity, IWeather, ICoordinateEntity, IWebhookEntity
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

        [NotMapped]
        public bool SendWebhook { get; set; }

        #endregion

        #region Constructors

        public Weather()
        {
        }

        public Weather(ClientWeatherProto weatherData)
        {
            var now = DateTime.UtcNow.ToTotalSeconds();
            var s2cell = weatherData.S2CellId.S2CellFromId();
            var center = s2cell.RectBound.Center;
            var alert = weatherData.Alerts?.FirstOrDefault();
            Id = weatherData.S2CellId;
            Level = s2cell.Level;
            Latitude = center.LatDegrees;
            Longitude = center.LngDegrees;
            GameplayCondition = weatherData.GameplayWeather.GameplayCondition;
            WindDirection = Convert.ToUInt16(weatherData.DisplayWeather.WindDirection);
            CloudLevel = Convert.ToUInt16(weatherData.DisplayWeather.CloudLevel);
            RainLevel = Convert.ToUInt16(weatherData.DisplayWeather.RainLevel);
            WindLevel = Convert.ToUInt16(weatherData.DisplayWeather.WindLevel);
            SnowLevel = Convert.ToUInt16(weatherData.DisplayWeather.SnowLevel);
            FogLevel = Convert.ToUInt16(weatherData.DisplayWeather.FogLevel);
            SpecialEffectLevel = Convert.ToUInt16(weatherData.DisplayWeather.SpecialEffectLevel);
            Severity = Convert.ToUInt16(weatherData.Alerts?.FirstOrDefault()?.Severity ?? null);
            WarnWeather = alert?.WarnWeather;
            Updated = now;
        }

        #endregion

        #region Public Methods

        public async Task UpdateAsync(MapDbContext context, IMemoryCacheHostedService memCache)
        {
            var now = DateTime.UtcNow.ToTotalSeconds();
            Updated = now;

            Weather? oldWeather = null;
            try
            {
                // Check cache first for client weather entity
                var cached = memCache.Get<long, Weather>(Id);
                if (cached != null)
                {
                    oldWeather = cached;
                }
                else
                {
                    oldWeather = await context.Weather.FindAsync(Id);
                    if (oldWeather != null)
                    {
                        memCache.Set(Id, oldWeather);
                    }
                    else
                    {
                        memCache.Set(Id, this);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Weather: {ex}");
            }

            if (oldWeather == null)
            {
                SendWebhook = true;
            }
            else if (oldWeather.GameplayCondition != GameplayCondition || 
                     oldWeather.WarnWeather != WarnWeather)
            {
                SendWebhook = true;
            }

            // Cache weather cell entity by id
            memCache.Set(Id, this);
        }

        public dynamic? GetWebhookData(string type)
        {
            switch (type.ToLower())
            {
                case "weather":
                    var s2cell = Id.S2CellFromId();
                    var polygon = new List<List<double>>();
                    for (var i = 0; i <= 3; i++)
                    {
                        var vertex = s2cell.GetVertex(i);
                        var coord = vertex.ToCoordinate();
                        polygon.Add(new List<double> { coord.Latitude, coord.Longitude });
                    }
                    return new
                    {
                        type = WebhookHeaders.Weather,
                        message = new
                        {
                            s2_cell_id = Id,
                            latitude = Latitude,
                            longitude = Longitude,
                            polygon,
                            gameplay_condition = GameplayCondition,
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

            Console.WriteLine($"Received unknown weather webhook payload type: {type}, returning null");
            return null;
        }

        #endregion
    }
}