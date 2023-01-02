namespace ChuckDeviceController.Data;

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
    name=COALESCE(VALUES(name), name),
    url=COALESCE(VALUES(url), url),
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
    name={1},
    url={2},
    updated=UNIX_TIMESTAMP()
WHERE id={0}
";

    #endregion

    #region Gym Trainer Queries

    public const string GymTrainerOnMergeUpdate = @"
INSERT INTO gym_trainer (
    name, level, team_id, battles_won, km_walked, pokemon_caught, experience
    combat_rank, combat_rating, has_shared_ex_pass, gym_badge_type, updated
)
VALUES {0}
ON DUPLICATE KEY UPDATE
    name=VALUES(name),
    level=VALUES(level),
    team_id=VALUES(team_id),
    battles_won=VALUES(battles_won),
    km_walked=VALUES(km_walked),
    pokemon_caught=VALUES(pokemon_caught),
    experience=VALUES(experience),
    combat_rank=VALUES(combat_rank),
    combat_rating=VALUES(combat_rating),
    has_shared_ex_pass=VALUES(has_shared_ex_pass),
    gym_badge_type=VALUES(gym_badge_type),
    updated=VALUES(updated)
";
    /// <summary>
    /// 0 - name
    /// 1 - level
    /// 2 - team_id
    /// 3 - battles_won
    /// 4 - km_walked
    /// 5 - pokemon_caught
    /// 6 - experience
    /// 7 - combat_rank
    /// 8 - combat_rating
    /// 9 - has_shared_ex_pass
    /// 10 - gym_badge_type
    /// 11 - updated
    /// </summary>
    public const string GymTrainerValuesRaw = @"
{0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9},
{10}, {11}
";

    #endregion

    #region Gym Defenders Queries

    public const string GymDefenderOnMergeUpdate = @"
INSERT INTO gym_defender (
    id, nickname, pokemon_id, display_pokemon_id, form, costume, gender, cp_when_deployed, cp_now, cp,
    battles_won, battles_lost, berry_value, times_fed, deployment_duration, trainer_name, fort_id, atv_iv, def_iv, sta_iv,
    move_1, move_2, move_3, battles_attacked, battles_defended, buddy_km_walked, buddy_candy_awarded, coins_returned, from_fort, hatched_from_egg,
    is_bad, is_egg, is_lucky, shiny, pvp_combat_won, pvp_combat_total, npc_combat_won, npc_combat_total, height_m, weight_kg,
    updated
)
VALUES {0}
ON DUPLICATE KEY UPDATE
    nickname=VALUES(nickname),
    cp_when_deployed=VALUES(cp_when_deployed),
    cp_now=VALUES(cp_now),
    cp=VALUES(cp),
    battles_won=VALUES(battles_won),
    battles_lost=VALUES(battles_lost),
    berry_value=VALUES(berry_value),
    times_fed=VALUES(times_fed),
    deployment_duration=VALUES(deployment_duration),
    trainer_name=VALUES(trainer_name),
    fort_id=VALUES(fort_id),
    move_1=VALUES(move_1),
    move_2=VALUES(move_2),
    move_3=VALUES(move_3),
    battles_attacked=VALUES(battles_attacked),
    battles_defended=VALUES(battles_defended),
    buddy_km_walked=VALUES(buddy_km_walked),
    buddy_candy_awarded=VALUES(buddy_candy_awarded),
    coins_returned=VALUES(coins_returned),
    pvp_combat_won=VALUES(pvp_combat_won),
    pvp_combat_total=VALUES(pvp_combat_total),
    npc_combat_won=VALUES(npc_combat_won),
    npc_combat_total=VALUES(npc_combat_total),
    height_m=VALUES(height_m),
    weight_kg=VALUES(weight_kg),
    updated=VALUES(updated)
";
    /// <summary>
    /// 0 - id
    /// 1 - nickname
    /// 2 - pokemon_id
    /// 3 - display_pokemon_id
    /// 4 - form
    /// 5 - costume
    /// 6 - gender
    /// 7 - cp_when_deployed
    /// 8 - cp_now
    /// 9 - cp
    /// 10 - battles_won
    /// 11 - battles_lost
    /// 12 - berry_value
    /// 13 - times_fed
    /// 14 - deployment_duration
    /// 15 - trainer_name
    /// 16 - fort_id
    /// 17 - atk_iv
    /// 18 - def_iv
    /// 19 - sta_iv
    /// 20 - move_1
    /// 21 - move_2
    /// 22 - move_3
    /// 23 - battles_attacked
    /// 24 - battles_defended
    /// 25 - buddy_km_walked
    /// 26 - buddy_candy_awarded
    /// 27 - coins_returned
    /// 28 - from_fort
    /// 29 - hatched_from_egg
    /// 30 - is_bad
    /// 31 - is_egg
    /// 32 - is_lucky
    /// 33 - shiny
    /// 34 - pvp_combat_won
    /// 35 - pvp_combat_total
    /// 36 - npc_combat_won
    /// 37 - npc_combat_total
    /// 38 - height_m
    /// 39 - weight_kg
    /// 40 - updated
    /// </summary>
    public const string GymDefenderValuesRaw = @"
{0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9},
{10}, {11}, {12}, {13}, {14}, {15}, {16}, {17}, {18}, {19},
{20}, {21}, {22}, {23}, {24}, {25}, {26}, {27}, {28}, {29},
{30}, {31}, {32}, {33}, {34}, {35}, {36}, {37}, {38}, {39},
{40}
";

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
    public const string DeviceValues = @"(
    @Uuid, @InstanceName, @AccountUsername,
    @LastHost, @LastLatitude, @LastLongitude, @LastSeen,
    @IsPendingAccountSwitch
)
";

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
    name=COALESCE(VALUES(name), name),
    url=COALESCE(VALUES(url), url),
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
    name={1},
    url={2},
    updated=UNIX_TIMESTAMP()
WHERE id={0}
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
    atk_iv=COALESCE(VALUES(atk_iv), atk_iv),
    def_iv=COALESCE(VALUES(def_iv), def_iv),
    sta_iv=COALESCE(VALUES(sta_iv), sta_iv),
    move_1=COALESCE(VALUES(move_1), move_1),
    move_2=COALESCE(VALUES(move_2), move_2),
    gender=COALESCE(VALUES(gender), gender),
    form=COALESCE(VALUES(form), form),
    costume=COALESCE(VALUES(costume), costume),
    cp=COALESCE(VALUES(cp), cp),
    level=COALESCE(VALUES(level), level),
    weight=COALESCE(VALUES(weight), weight),
    size=COALESCE(VALUES(size), size),
    weather=VALUES(weather),
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
    pvp=COALESCE(VALUES(pvp), pvp)
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
    atk_iv=COALESCE(VALUES(atk_iv), atk_iv),
    def_iv=COALESCE(VALUES(def_iv), def_iv),
    sta_iv=COALESCE(VALUES(sta_iv), sta_iv),
    move_1=COALESCE(VALUES(move_1), move_1),
    move_2=COALESCE(VALUES(move_2), move_2),
    gender=COALESCE(VALUES(gender), gender),
    form=COALESCE(VALUES(form), form),
    costume=COALESCE(VALUES(costume), costume),
    cp=COALESCE(VALUES(cp), cp),
    level=COALESCE(VALUES(level), level),
    weight=COALESCE(VALUES(weight), weight),
    size=COALESCE(VALUES(size), size),
    weather=VALUES(weather),
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
    pvp=COALESCE(VALUES(pvp), pvp)
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

    public const string AccountOnMergeUpdate = @"
INSERT INTO account (
    username, password, first_warning_timestamp, failed_timestamp, failed, level,
    last_encounter_time, last_encounter_lat, last_encounter_lon, spins, tutorial,
    creation_timestamp, warn, warn_expire_timestamp, warn_message_acknowledged,
    suspended_message_acknowledged, was_suspended, banned, last_used_timestamp,
    `group`
)
VALUES
    {0}
ON DUPLICATE KEY UPDATE
    first_warning_timestamp=VALUES(first_warning_timestamp),
    failed_timestamp=VALUES(failed_timestamp),
    failed=VALUES(failed),
    level=VALUES(level),
    last_encounter_time=VALUES(last_encounter_time),
    last_encounter_lat=VALUES(last_encounter_lat),
    last_encounter_lon=VALUES(last_encounter_lon),
    spins=VALUES(spins),
    tutorial=VALUES(tutorial),
    creation_timestamp=VALUES(creation_timestamp),
    warn=VALUES(warn),
    warn_expire_timestamp=VALUES(warn_expire_timestamp),
    warn_message_acknowledged=VALUES(warn_message_acknowledged),
    suspended_message_acknowledged=VALUES(suspended_message_acknowledged),
    was_suspended=VALUES(was_suspended),
    banned=VALUES(banned),
    last_used_timestamp=VALUES(last_used_timestamp),
    `group`=VALUES(`group`)
";
    /// <summary>
    /// 0 - username
    /// 1 - password
    /// 2 - first_warning_timestamp
    /// 3 - failed_timestamp
    /// 4 - failed
    /// 5 - level
    /// 6 - last_encounter_time
    /// 7 - last_encounter_lat
    /// 8 - last_encounter_lon
    /// 9 - spins
    /// 10 - tutorial
    /// 11 - creation_timestamp
    /// 12 - warn
    /// 13 - warn_expire_timestamp
    /// 14 - warn_message_acknowledged
    /// 15 - suspended_message_acknowledged
    /// 16 - was_suspended
    /// 17 - banned
    /// 18 - last_used_timestamp
    /// 19 - group
    /// </summary>
    public const string AccountValuesRaw = @"
(
    {0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9},
    {10}, {11}, {12}, {13}, {14}, {15}, {16}, {17}, {18}, {19}
)
";

    public const string AccountLevelUpdate = @"
UPDATE account
SET level = @Level
WHERE username = @Username
";

    #endregion
}