namespace ChuckDeviceController
{
    // TODO: Finish queries
    public static class SqlQueries
    {
        #region Gym Queries

        public const string GymOnMergeUpdate = @"
INSERT INTO gym (
    guarding_pokemon_id, available_slots, team_id, in_battle, ex_raid_eligible, raid_level, raid_end_timestamp,
    raid_spawn_timestamp, raid_battle_timestamp, raid_pokemon_id, raid_pokemon_move_1, raid_pokemon_move_2, 
    raid_pokemon_form, raid_pokemon_costume, raid_pokemon_cp, raid_pokemon_evolution, raid_pokemon_gender,
    raid_is_exclusive, total_cp, sponsor_id, ar_scan_eligible,
    id, lat, lon, name, url, enabled, deleted, cell_id, power_up_points, power_up_level, power_up_end_timestamp,
    first_seen_timestamp, last_modified_timestamp, updated
)
VALUES {0}
ON DUPLICATE KEY UPDATE
    lat=VALUES(lat),
    lon=VALUES(lon),
    name=VALUES(name),
    url=VALUES(url),
    enabled=VALUES(enabled),
    deleted=VALUES(deleted),
    cell_id=VALUES(cell_id),
    power_up_points=VALUES(power_up_points),
    power_up_level=VALUES(power_up_level),
    power_up_end_timestamp=VALUES(power_up_end_timestamp),
    first_seen_timestamp=VALUES(first_seen_timestamp),
    last_modified_timestamp=VALUES(last_modified_timestamp),
    updated=VALUES(updated),
    guarding_pokemon_id=VALUES(guarding_pokemon_id),
    available_slots=VALUES(available_slots),
    team_id=VALUES(team_id),
    in_battle=VALUES(in_battle),
    ex_raid_eligible=VALUES(ex_raid_eligible),
    raid_level=VALUES(raid_level),
    raid_end_timestamp=VALUES(raid_end_timestamp),
    raid_spawn_timestamp=VALUES(raid_spawn_timestamp),
    raid_battle_timestamp=VALUES(raid_battle_timestamp),
    raid_pokemon_id=VALUES(raid_pokemon_id),
    raid_pokemon_move_1=VALUES(raid_pokemon_move_1),
    raid_pokemon_move_2=VALUES(raid_pokemon_move_2), 
    raid_pokemon_form=VALUES(raid_pokemon_form),
    raid_pokemon_costume=VALUES(raid_pokemon_costume),
    raid_pokemon_cp=VALUES(raid_pokemon_cp),
    raid_pokemon_evolution=VALUES(raid_pokemon_evolution),
    raid_pokemon_gender=VALUES(raid_pokemon_gender),
    raid_is_exclusive=VALUES(raid_is_exclusive),
    total_cp=VALUES(total_cp),
    sponsor_id=VALUES(sponsor_id),
    ar_scan_eligible=VALUES(ar_scan_eligible)
";
        /// <summary>
        /// 0 - guarding_pokemon_id
        /// 1 - available_slots
        /// 2 - team_id
        /// 3 - in_battle,
        /// 4 - ex_raid_eligible
        /// 5 - raid_level
        /// 6 - raid_end_timestamp
        /// 7 - raid_spawn_timestamp
        /// 8 - raid_battle_timestamp
        /// 9 - raid_pokemon_id
        /// 10 - raid_pokemon_move_1,
        /// 11 - raid_pokemon_move_2
        /// 12 - raid_pokemon_form
        /// 13 - raid_pokemon_costume
        /// 14 - raid_pokemon_cp,
        /// 15 - raid_pokemon_evolution
        /// 16 - raid_pokemon_gender
        /// 17 - raid_is_exclusive
        /// 18 - total_cp
        /// 19 - sponsor_id
        /// 20 - ar_scan_eligible
        /// 21 - id
        /// 22 - lat
        /// 23 - lon
        /// 24 - name
        /// 25 - url
        /// 26 - enabled
        /// 27 - deleted
        /// 28 - cell_id
        /// 29 - power_up_points
        /// 30 - power_up_level
        /// 31 - power_up_end_timestamp
        /// 32 - first_seen_timestamp
        /// 33 - last_modified_timestamp
        /// 34 - updated
        /// </summary>
        public const string GymValuesRaw = @"
(
    {0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10},
    {11}, {12}, {13}, {14}, {15}, {16}, {17}, {18}, {19}, {20},
    {21}, {22}, {23}, {24}, {25}, {26}, {27}, {28}, {29}, {30},
    {31}, {32}, {33}, {34}
)
";

        public const string GymDetailsOnMergeUpdate = @"
UPDATE gym
SET
    name=`{1}`,
    url=`{2}`,
    updated=UNIX_TIMESTAMP()
WHERE id='{0}'
";

        public const string GymTrainerOnMergeUpdate = @"";
        public const string GymTrainerValuesRaw = @"";

        public const string GymDefenderOnMergeUpdate = @"";
        public const string GymDefenderValuesRaw = @"";

        #endregion

        #region Device Queries

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

        #endregion

        #region Pokestop Queries

        public const string PokestopOnMergeUpdate = @"
INSERT INTO pokestop (
    lure_id, lure_expire_timestamp, sponsor_id, ar_scan_eligible,
    quest_type, quest_template, quest_title, quest_target, quest_timestamp, quest_conditions, quest_rewards,
    alternative_quest_type, alternative_quest_template, alternative_quest_title, alternative_quest_target,
    alternative_quest_timestamp, alternative_quest_conditions, alternative_quest_rewards,
    id, lat, lon, name, url, enabled, deleted, cell_id, power_up_points, power_up_level, power_up_end_timestamp,
    first_seen_timestamp, last_modified_timestamp, updated
)
VALUES
    {0}
ON DUPLICATE KEY UPDATE
    lat=VALUES(lat),
    lon=VALUES(lon),
    name=VALUES(name),
    url=VALUES(url),
    enabled=VALUES(enabled),
    deleted=VALUES(deleted),
    cell_id=VALUES(cell_id),
    power_up_points=VALUES(power_up_points),
    power_up_level=VALUES(power_up_level),
    power_up_end_timestamp=VALUES(power_up_end_timestamp),
    first_seen_timestamp=VALUES(first_seen_timestamp),
    last_modified_timestamp=VALUES(last_modified_timestamp),
    updated=VALUES(updated),
    lure_id=VALUES(lure_id),
    lure_expire_timestamp=VALUES(lure_expire_timestamp),
    sponsor_id=VALUES(sponsor_id),
    ar_scan_eligible=VALUES(ar_scan_eligible),
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
    lure_id, lure_expire_timestamp, sponsor_id, ar_scan_eligible,
    quest_type, quest_template, quest_title, quest_target, quest_timestamp, quest_conditions, quest_rewards,
    alternative_quest_type, alternative_quest_template, alternative_quest_title, alternative_quest_target,
    alternative_quest_timestamp, alternative_quest_conditions, alternative_quest_rewards,
    id, lat, lon, name, url, enabled, deleted, cell_id, power_up_points, power_up_level, power_up_end_timestamp,
    first_seen_timestamp, last_modified_timestamp, updated
)
VALUES
    {0}
ON DUPLICATE KEY UPDATE
    lat=VALUES(lat),
    lon=VALUES(lon),
    name=VALUES(name),
    url=VALUES(url),
    enabled=VALUES(enabled),
    deleted=VALUES(deleted),
    cell_id=VALUES(cell_id),
    power_up_points=VALUES(power_up_points),
    power_up_level=VALUES(power_up_level),
    power_up_end_timestamp=VALUES(power_up_end_timestamp),
    first_seen_timestamp=VALUES(first_seen_timestamp),
    last_modified_timestamp=VALUES(last_modified_timestamp),
    updated=VALUES(updated),
    lure_id=VALUES(lure_id),
    lure_expire_timestamp=VALUES(lure_expire_timestamp),
    sponsor_id=VALUES(sponsor_id),
    ar_scan_eligible=VALUES(ar_scan_eligible)
";

        public const string PokestopDetailsOnMergeUpdate = @"
UPDATE pokestop
SET
    name=`{1}`,
    url=`{2}`,
    updated=UNIX_TIMESTAMP()
WHERE id='{0}'
";

        /// <summary>
        /// 0 - lure_id
        /// 1 - lure_expire_timestamp
        /// 2 - sponsor_id
        /// 3 - ar_scan_eligible,
        /// 4 - quest_type
        /// 5 - quest_template
        /// 6 - quest_title
        /// 7 - quest_target
        /// 8 - quest_timestamp
        /// 9 - quest_conditions
        /// 10 - quest_rewards,
        /// 11 - alternative_quest_type
        /// 12 - alternative_quest_template
        /// 13 - alternative_quest_title
        /// 14 - alternative_quest_target,
        /// 15 - alternative_quest_timestamp
        /// 16 - alternative_quest_conditions
        /// 17 - alternative_quest_rewards
        /// 18 - id
        /// 19 - lat
        /// 20 - lon
        /// 21 - name
        /// 22 - url
        /// 23 - enabled
        /// 24 - deleted
        /// 25 - cell_id
        /// 26 - power_up_points
        /// 27 - power_up_level
        /// 28 - power_up_end_timestamp
        /// 29 - first_seen_timestamp
        /// 30 - last_modified_timestamp
        /// 31 - updated
        /// </summary>
        public const string PokestopValuesRaw = @"
(
    {0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10},
    {11}, {12}, {13}, {14}, {15}, {16}, {17}, {18}, {19}, {20},
    {21}, {22}, {23}, {24}, {25}, {26}, {27}, {28}, {29}, {30},
    {31}
)
";

        #endregion

        #region Incident Queries

        public const string IncidentOnMergeUpdate = @"
INSERT INTO incident (id, pokestop_id, start, expiration, display_type, style, `character`, updated)
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
        /// <summary>
        /// 0 - id
        /// 1 - pokestop_id
        /// 2 - start
        /// 3 - expiration
        /// 4 - display_type
        /// 5 - style
        /// 6 - character
        /// 7 - updated
        /// </summary>
        public const string IncidentValuesRaw = "({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7})";

        #endregion

        #region Pokemon Queries

        public const string PokemonOnMergeUpdate = @"
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
    seen_type=VALUES(seen_type)
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
    seen_type=VALUES(seen_type)
";

        /// <summary>
        /// 0 - id
        /// 1 - pokemon_id
        /// 2 - lat
        /// 3 - lon
        /// 4 - spawn_id
        /// 5 - expire_timestamp
        /// 6 - atk_iv
        /// 7 - def_iv
        /// 8 - sta_iv
        /// 9 - move_1
        /// 10 - move_2
        /// 11 - gender
        /// 12 - form
        /// 13 - costume
        /// 14 - cp
        /// 15 - level
        /// 16 - weight
        /// 17 - size
        /// 18 - weather
        /// 19 - shiny
        /// 20 - username
        /// 21 - pokestop_id
        /// 22 - first_seen_timestamp
        /// 23 - updated
        /// 24 - changed
        /// 25 - cell_id
        /// 26 - expire_timestamp_verified
        /// 27 - capture_1
        /// 28 - capture_2
        /// 29 - capture_3
        /// 30 - is_ditto
        /// 31 - display_pokemon_id
        /// 32 - base_height
        /// 33 - base_weight
        /// 34 - is_event
        /// 35 - seen_type
        /// 36 - pvp
        /// </summary>
        public const string PokemonValuesRaw = @"
(
    {0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10},
    {11}, {12}, {13}, {14}, {15}, {16}, {17}, {18}, {19}, {20},
    {21}, {22}, {23}, {24}, {25}, {26}, {27}, {28}, {29}, {30},
    {31}, {32}, {33}, {34}, {35}, {36}
)";

        #endregion

        #region Spawnpoint Queries

        public const string SpawnpointOnMergeUpdate = @"
INSERT INTO spawnpoint (id, lat, lon, despawn_sec, last_seen, updated)
VALUES
    {0}
ON DUPLICATE KEY UPDATE
    despawn_sec=VALUES(despawn_sec),
    last_seen=VALUES(last_seen),
    updated=VALUES(updated)
";
        //public const string SpawnpointValues = "(@Id, @Latitude, @Longitude, @DespawnSecond, @LastSeen, @Updated)";
        //public const string SpawnpointValuesRaw = "({0}, {1}, {2}, {3}, {4}, UNIX_TIMESTAMP())";
        public const string SpawnpointValuesRaw = "({0}, {1}, {2}, {3}, {4}, {5})";

        #endregion

        #region Cell Queries

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

        //public const string CellValues = "(@Id, @Level, @Latitude, @Longitude, UNIX_TIMESTAMP())";
        //public const string CellValuesRaw = "({0}, {1}, {2}, {3}, UNIX_TIMESTAMP())";
        public const string CellValuesRaw = "({0}, {1}, {2}, {3}, {4})";
        //public const string CellOnUpdateValues = "center_lat=@Latitude, center_lon=@Longitude, updated=UNIX_TIMESTAMP()";

        #endregion

        #region Weather Queries

        public const string WeatherOnMergeUpdate = @"
INSERT INTO weather (
    id, level, latitude, longitude, gameplay_condition, cloud_level, rain_level,
    snow_level, fog_level, wind_level, wind_direction, warn_weather, special_effect_level,
    severity, updated
)
VALUES
    {0}
ON DUPLICATE KEY UPDATE
    level=VALUES(level),
    latitude=VALUES(latitude),
    longitude=VALUES(longitude),
    gameplay_condition=VALUES(gameplay_condition),
    cloud_level=VALUES(cloud_level),
    rain_level=VALUES(rain_level),
    snow_level=VALUES(snow_level),
    fog_level=VALUES(fog_level),
    wind_level=VALUES(wind_level),
    wind_direction=VALUES(wind_direction),
    warn_weather=VALUES(warn_weather),
    special_effect_level=VALUES(special_effect_level),
    severity=VALUES(severity),
    updated=VALUES(updated)
";
        public const string WeatherValues = @"
(
    @Id, @Level, @Latitude, @Longitude, @GameplayCondition, @CloudLevel, @RainLevel, @SnowLevel, @FogLevel,
    @WindLevel, @WindDirection, @WarnWeather, @SpecialEffectLevel, @Severity, @Updated
)
";
        public const string WeatherValuesRaw = @"
(
    {0}, {1}, {2}, {3}, {4}, {5}, {6}, {7},
    {8}, {9}, {10}, {11}, {12}, {13}, {14}
)
";

        #endregion

        #region Account Queries

        public const string AccountOnMergeUpdate = @"";
        public const string AccountValuesRaw = @"";

        public const string AccountLevelUpdate = @"
UPDATE account
SET level = @Level
WHERE username = @Username
";

        #endregion
    }
}