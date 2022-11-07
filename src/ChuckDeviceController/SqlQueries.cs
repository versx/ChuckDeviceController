namespace ChuckDeviceController
{
    public static class SqlQueries
    {
        public const string GymOptions = @"";
        public const string GymDetailsOnMergeUpdate = @"";
        public const string GymTrainerOnMergeUpdate = @"";
        public const string GymDefenderOnMergeUpdate = @"";

        public const string DeviceOnMergeUpdate = @"
INSERT INTO device (
    uuid, instance_name, account_username,
    last_host, last_lat, last_lon, last_seen,
    pending_account_switch
)
VALUES
    {0}
ON DUPLICATE KEY UPDATE
    instance_name=VALUES(instance_name),
    account_username=VALUES(account_username),
    last_host=VALUES(last_host),
    last_lat=VALUES(last_lat),
    last_lon=VALUES(last_lon),
    last_seen=VALUES(last_seen),
    pending_account_switch=VALUES(pending_account_switch)
";

        public const string DeviceValues = "(@Uuid, @InstanceName, @AccountUsername, @LastHost, @LastLatitude, @LastLongitude, @LastSeen, @IsPendingAccountSwitch)";

        #region Pokestop Queries

        public const string PokestopOptions = @"
INSERT INTO pokestop (
    id, lat, lon, name, url, lure_id, lure_expire_timestamp, updated, enabled, cell_id, deleted,
    first_seen_timestamp, sponsor_id, ar_scan_eligible, power_up_points, power_up_level, power_up_end_timestamp,
    quest_type, quest_template, quest_title, quest_target, quest_timestamp, quest_conditions, quest_rewards,
    alternative_quest_type, alternative_quest_template, alternative_quest_title, alternative_quest_target,
    alternative_quest_timestamp, alternative_quest_conditions, alternative_quest_rewards
)
VALUES
    {0}
ON DUPLICATE KEY UPDATE
    lat=VALUES(lat),
    lon=VALUES(lon),
    name=VALUES(name),
    url=VALUES(url),
    lure_id=VALUES(name),
    lure_expire_timestamp=VALUES(name),
    updated=VALUES(name),
    enabled=VALUES(name),
    cell_id=VALUES(name),
    deleted=VALUES(name),
    first_seen_timestamp=VALUES(name),
    sponsor_id=VALUES(name),
    ar_scan_eligible=VALUES(name),
    power_up_points=VALUES(power_up_points),
    power_up_level=VALUES(power_up_level),
    power_up_end_timestamp=VALUES(power_up_end_timestamp),
    quest_type=VALUES(quest_type),
    quest_template=VALUES(quest_template),
    quest_title=VALUES(quest_title),
    quest_target=VALUES(quest_target),
    quest_timestamp=VALUES(quest_timestamp),
    quest_conditions=VALUES(quest_conditions),
    quest_rewards=VALUES(quest_rewards),
    alternative_quest_type=VALUES(alternative_quest_type),
    alternative_quest_template=VALUES(alternative_quest_template),
    alternative_quest_title=VALUES(alternative_quest_title),
    alternative_quest_target=VALUES(alternative_quest_target),
    alternative_quest_timestamp=VALUES(alternative_quest_timestamp),
    alternative_quest_conditions=VALUES(alternative_quest_conditions),
    alternative_quest_rewards=VALUES(alternative_quest_rewards)
";

        // TODO: Only update name/url in ON DUP section if not null
        public const string PokestopIgnoreOnMerge = @"
INSERT INTO pokestop (
    id, lat, lon, name, url, lure_id, lure_expire_timestamp, updated, enabled, cell_id, deleted,
    first_seen_timestamp, sponsor_id, ar_scan_eligible, power_up_points, power_up_level, power_up_end_timestamp,
    quest_type, quest_template, quest_title, quest_target, quest_timestamp, quest_conditions, quest_rewards,
    alternative_quest_type, alternative_quest_template, alternative_quest_title, alternative_quest_target,
    alternative_quest_timestamp, alternative_quest_conditions, alternative_quest_rewards
)
VALUES
    {0}
ON DUPLICATE KEY UPDATE
    lat=VALUES(lat),
    lon=VALUES(lon),
    name=VALUES(name),
    url=VALUES(url),
    lure_id=VALUES(name),
    lure_expire_timestamp=VALUES(name),
    updated=VALUES(name),
    enabled=VALUES(name),
    cell_id=VALUES(name),
    deleted=VALUES(name),
    first_seen_timestamp=VALUES(name),
    sponsor_id=VALUES(name),
    ar_scan_eligible=VALUES(name),
    power_up_points=VALUES(power_up_points),
    power_up_level=VALUES(power_up_level),
    power_up_end_timestamp=VALUES(power_up_end_timestamp)
";

        public const string PokestopDetailsOnMergeUpdate = @"
UPDATE pokestop
SET
    name=`{1}`,
    url=`{2}`,
    updated=UNIX_TIMESTAMP()
WHERE id='{0}'
";

        public const string IncidentOnMergeUpdate = @"
INSERT INTO incident (`id`, `pokestop_id`, `start`, `expiration`, `display_type`, `style`, `character`, `updated`)
VALUES
    {0}
ON DUPLICATE KEY UPDATE
    pokestop_id=VALUES(pokestop_id),
    start=VALUES(start),
    expiration=VALUES(expiration),
    display_type=VALUES(display_type),
    style=VALUES(style),
    `character`=VALUES(`character`),
    updated=VALUES(updated)
";

        #endregion

        #region Pokemon Queries

        public const string PokemonOptions = @"
INSERT INTO pokemon (
    id, pokemon_id, lat, lon, spawn_id, expire_timestamp,
    atk_iv, def_iv, sta_iv, move_1, move_2, gender,
    form, costume, cp, level, weight, size, weather, shiny,
    username, pokestop_id, first_seen_timestamp, updated,
    changed, cell_id, expire_timestamp_verified, capture_1,
    capture_2, capture_3, is_ditto, display_pokemon_id,
    base_height, base_weight, is_event, seen_type, pvp
)
VALUES
    {0}
";

        public const string PokemonIgnoreOnMerge = @"
INSERT INTO pokemon (
    id, pokemon_id, lat, lon, spawn_id, expire_timestamp,
    atk_iv, def_iv, sta_iv, move_1, move_2, gender,
    form, costume, cp, level, weight, size, weather, shiny,
    username, pokestop_id, first_seen_timestamp, updated,
    changed, cell_id, expire_timestamp_verified, capture_1,
    capture_2, capture_3, is_ditto, display_pokemon_id,
    base_height, base_weight, is_event, seen_type, pvp
)
VALUES
    {0}
ON DUPLICATE KEY UPDATE
    spawn_id=VALUES(spawn_id),
    expire_timestamp=VALUES(expire_timestamp),
    shiny=VALUES(shiny),
    username=VALUES(username),
    pokestop_id=VALUES(pokestop_id),
    first_seen_timestamp=VALUES(first_seen_timestamp),
    updated=VALUES(updated),
    changed=VALUES(changed),
    cell_id=VALUES(cell_id),
    expire_timestamp_verified=VALUES(expire_timestamp_verified),
    capture_1=VALUES(capture_1),
    capture_2=VALUES(capture_2),
    capture_3=VALUES(capture_3),
    is_ditto=VALUES(is_ditto),
    display_pokemon_id=VALUES(display_pokemon_id),
    base_height=VALUES(base_height),
    base_weight=VALUES(base_weight),
    is_event=VALUES(is_event),
    seen_type=VALUES(seen_type),
    updated=VALUES(updated)
";

        public const string PokemonOnMergeUpdate = "";

        #endregion

        public const string SpawnpointOnMergeUpdate = @"
INSERT INTO spawnpoint (id, lat, lon, despawn_sec, last_seen, updated)
VALUES
    {0}
ON DUPLICATE KEY UPDATE
    last_seen=VALUES(last_seen),
    updated=VALUES(updated),
    despawn_sec=VALUES(despawn_sec)
";
        public const string SpawnpointValues = "(@Id, @Latitude, @Longitude, @DespawnSecond, @LastSeen, @Updated)";

        public const string CellOnMergeUpdate = @"
INSERT INTO s2cell (id, level, center_lat, center_lon, updated)
VALUES
    {0}
ON DUPLICATE KEY UPDATE
    level=VALUES(level),
    center_lat=VALUES(center_lat),
    center_lon=VALUES(center_lon),
    updated=VALUES(updated)
";

        public const string CellOnUpdate = @"
UPDATE s2cell SET {0}
WHERE id=@Id
";

        public const string CellValues = "(@Id, @Level, @Latitude, @Longitude, UNIX_TIMESTAMP())";
        public const string CellOnUpdateValues = "center_lat=@Latitude, center_lon=@Longitude, updated=UNIX_TIMESTAMP()";

        public const string WeatherOnMergeUpdate = @"
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
        public const string WeatherValues = @"
(
    @Id, @Level, @Latitude, @Longitude, @GameplayCondition, @CloudLevel, @RainLevel, @SnowLevel, @FogLevel,
    @WindLevel, @WindDirection, @WarnWeather, @SpecialEffectLevel, @Severity, @Updated
)
";

        public const string AccountOnMergeUpdate = @"";
    }
}
