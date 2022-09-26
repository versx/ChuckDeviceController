namespace ChuckDeviceController
{
    public static class SqlQueries
    {
        public const string S2Cells = @"
INSERT INTO s2cell (id, level, center_lat, center_lon, updated)
VALUES
    {0}
ON DUPLICATE KEY UPDATE
    level=VALUES(level),
    center_lat=VALUES(center_lat),
    center_lon=VALUES(center_lon),
    updated=VALUES(updated)
";

        public const string WeatherCells = @"
INSERT INTO weather (id, level, latitude, longitude, gameplay_condition, cloud_level, rain_level, snow_level, fog_level, wind_level, wind_direction, warn_weather, special_effect_level, severity, updated)
VALUES
    {0}
ON DUPLICATE KEY UPDATE
    level=VALUES(level),
    latitude=VALUES(latitude),
    longitude=VALUES(longitude),
    gameplay_condition=VALUES(gameplay_condition),
    wind_direction=VALUES(wind_direction),
    cloud_level=VALUES(cloud_level),
    rain_level=VALUES(rain_level),
    wind_level=VALUES(wind_level),
    snow_level=VALUES(snow_level),
    fog_level=VALUES(fog_level),
    special_effect_level=VALUES(special_effect_level),
    severity=VALUES(severity),
    warn_weather=VALUES(warn_weather),
    updated=VALUES(updated)
";
    }
}
