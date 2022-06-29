ALTER DATABASE CHARACTER SET utf8mb4;


CREATE TABLE `gym` (
    `id` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `lat` double NOT NULL,
    `lon` double NOT NULL,
    `name` longtext CHARACTER SET utf8mb4 NULL,
    `url` longtext CHARACTER SET utf8mb4 NULL,
    `last_modified_timestamp` bigint unsigned NOT NULL,
    `raid_end_timestamp` bigint unsigned NULL,
    `raid_spawn_timestamp` bigint unsigned NULL,
    `raid_battle_timestamp` bigint unsigned NULL,
    `updated` bigint unsigned NOT NULL,
    `raid_pokemon_id` int unsigned NULL,
    `guarding_pokemon_id` int unsigned NOT NULL,
    `available_slots` smallint unsigned NOT NULL,
    `team_id` int NOT NULL,
    `raid_level` smallint unsigned NULL,
    `enabled` tinyint(1) NOT NULL,
    `ex_raid_eligible` tinyint(1) NOT NULL,
    `in_battle` tinyint(1) NOT NULL,
    `raid_pokemon_move_1` int unsigned NULL,
    `raid_pokemon_move_2` int unsigned NULL,
    `raid_pokemon_form` int unsigned NULL,
    `raid_pokemon_costume` int unsigned NULL,
    `raid_pokemon_cp` int unsigned NULL,
    `raid_pokemon_evolution` int unsigned NULL,
    `raid_pokemon_gender` smallint unsigned NULL,
    `raid_is_exclusive` tinyint(1) NULL,
    `cell_id` bigint unsigned NOT NULL,
    `deleted` tinyint(1) NOT NULL,
    `total_cp` int NOT NULL,
    `first_seen_timestamp` bigint unsigned NOT NULL,
    `sponsor_id` int unsigned NULL,
    `ar_scan_eligible` tinyint(1) NULL,
    `power_up_points` int unsigned NULL,
    `power_up_level` smallint unsigned NULL,
    `power_up_end_timestamp` bigint unsigned NULL,
    CONSTRAINT `PK_gym` PRIMARY KEY (`id`)
) CHARACTER SET=utf8mb4;


CREATE TABLE `gym_defender` (
    `id` bigint unsigned NOT NULL,
    `nickname` longtext CHARACTER SET utf8mb4 NOT NULL,
    `pokemon_id` smallint unsigned NOT NULL,
    `display_pokemon_id` smallint unsigned NOT NULL,
    `form` smallint unsigned NOT NULL,
    `costume` smallint unsigned NOT NULL,
    `gender` int NOT NULL,
    `cp_when_deployed` int unsigned NOT NULL,
    `cp_now` int unsigned NOT NULL,
    `cp` int unsigned NOT NULL,
    `battles_won` int unsigned NOT NULL,
    `battles_lost` int unsigned NOT NULL,
    `berry_value` double NOT NULL,
    `times_fed` int unsigned NOT NULL,
    `deployment_duration` bigint unsigned NOT NULL,
    `trainer_name` longtext CHARACTER SET utf8mb4 NOT NULL,
    `fort_id` longtext CHARACTER SET utf8mb4 NOT NULL,
    `atk_iv` smallint unsigned NOT NULL,
    `def_iv` smallint unsigned NOT NULL,
    `sta_iv` smallint unsigned NOT NULL,
    `move_1` smallint unsigned NOT NULL,
    `move_2` smallint unsigned NOT NULL,
    `move_3` smallint unsigned NOT NULL,
    `battles_attacked` int unsigned NOT NULL,
    `battles_defended` int unsigned NOT NULL,
    `buddy_km_walked` double NOT NULL,
    `buddy_candy_awarded` int unsigned NOT NULL,
    `coins_returned` int unsigned NOT NULL,
    `from_fort` tinyint(1) NOT NULL,
    `hatched_from_egg` tinyint(1) NOT NULL,
    `is_bad` tinyint(1) NOT NULL,
    `is_egg` tinyint(1) NOT NULL,
    `is_lucky` tinyint(1) NOT NULL,
    `shiny` tinyint(1) NOT NULL,
    `pvp_combat_won` int unsigned NOT NULL,
    `pvp_combat_total` int unsigned NOT NULL,
    `npc_combat_won` int unsigned NOT NULL,
    `npc_combat_total` int unsigned NOT NULL,
    `height_m` double NOT NULL,
    `weight_kg` double NOT NULL,
    `updated` bigint unsigned NOT NULL,
    CONSTRAINT `PK_gym_defender` PRIMARY KEY (`id`)
) CHARACTER SET=utf8mb4;


CREATE TABLE `gym_trainer` (
    `name` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `level` smallint unsigned NOT NULL,
    `team_id` int NOT NULL,
    `battles_won` int unsigned NOT NULL,
    `km_walked` double NOT NULL,
    `pokemon_caught` bigint unsigned NOT NULL,
    `experience` bigint unsigned NOT NULL,
    `combat_rank` bigint unsigned NOT NULL,
    `combat_rating` bigint unsigned NOT NULL,
    `has_shared_ex_pass` tinyint(1) NOT NULL,
    `gym_badge_type` smallint unsigned NOT NULL,
    `updated` bigint unsigned NOT NULL,
    CONSTRAINT `PK_gym_trainer` PRIMARY KEY (`name`)
) CHARACTER SET=utf8mb4;


CREATE TABLE `pokemon` (
    `id` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `pokemon_id` int unsigned NOT NULL,
    `lat` double NOT NULL,
    `lon` double NOT NULL,
    `spawn_id` bigint unsigned NULL,
    `expire_timestamp` bigint unsigned NOT NULL,
    `atk_iv` smallint unsigned NULL,
    `def_iv` smallint unsigned NULL,
    `sta_iv` smallint unsigned NULL,
    `iv` double AS ((`atk_iv` + `def_iv` + `sta_iv`) * 100 / 45),
    `move_1` smallint unsigned NULL,
    `move_2` smallint unsigned NULL,
    `gender` smallint unsigned NULL,
    `form` smallint unsigned NULL,
    `costume` smallint unsigned NULL,
    `cp` smallint unsigned NULL,
    `level` smallint unsigned NULL,
    `weight` double NULL,
    `size` double NULL,
    `weather` smallint unsigned NULL,
    `shiny` tinyint(1) NULL,
    `username` longtext CHARACTER SET utf8mb4 NULL,
    `pokestop_id` longtext CHARACTER SET utf8mb4 NULL,
    `first_seen_timestamp` bigint unsigned NULL,
    `updated` bigint unsigned NOT NULL,
    `changed` bigint unsigned NOT NULL,
    `cell_id` bigint unsigned NOT NULL,
    `expire_timestamp_verified` tinyint(1) NOT NULL,
    `capture_1` double NULL,
    `capture_2` double NULL,
    `capture_3` double NULL,
    `is_ditto` tinyint(1) NOT NULL,
    `display_pokemon_id` int unsigned NULL,
    `base_height` double NOT NULL,
    `base_weight` double NOT NULL,
    `is_event` tinyint(1) NOT NULL,
    `seen_type` longtext CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK_pokemon` PRIMARY KEY (`id`)
) CHARACTER SET=utf8mb4;


CREATE TABLE `pokestop` (
    `id` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `lat` double NOT NULL,
    `lon` double NOT NULL,
    `name` longtext CHARACTER SET utf8mb4 NULL,
    `url` longtext CHARACTER SET utf8mb4 NULL,
    `lure_id` int unsigned NOT NULL,
    `lure_expire_timestamp` bigint unsigned NULL,
    `last_modified_timestamp` bigint unsigned NOT NULL,
    `updated` bigint unsigned NOT NULL,
    `enabled` tinyint(1) NOT NULL,
    `cell_id` bigint unsigned NOT NULL,
    `deleted` tinyint(1) NOT NULL,
    `first_seen_timestamp` bigint unsigned NOT NULL,
    `sponsor_id` int unsigned NULL,
    `ar_scan_eligible` tinyint(1) NOT NULL,
    `power_up_points` int unsigned NULL,
    `power_up_level` smallint unsigned NULL,
    `power_up_end_timestamp` bigint unsigned NULL,
    `quest_type` int unsigned NULL,
    `quest_template` longtext CHARACTER SET utf8mb4 NULL,
    `quest_title` longtext CHARACTER SET utf8mb4 NULL,
    `quest_target` smallint unsigned NULL,
    `quest_timestamp` bigint unsigned NULL,
    `quest_reward_type` smallint unsigned AS (json_extract(json_extract(`quest_rewards`,'$[*].type'),'$[0]')),
    `quest_item_id` smallint unsigned AS (json_extract(json_extract(`quest_rewards`,'$[*].info.item_id'),'$[0]')),
    `quest_reward_amount` smallint unsigned AS (json_extract(json_extract(`quest_rewards`,'$[*].info.amount'),'$[0]')),
    `quest_pokemon_id` int unsigned AS (json_extract(json_extract(`quest_rewards`,'$[*].info.pokemon_id'),'$[0]')),
    `quest_conditions` longtext CHARACTER SET utf8mb4 NULL,
    `quest_rewards` longtext CHARACTER SET utf8mb4 NULL,
    `alternative_quest_type` int unsigned NULL,
    `alternative_quest_template` longtext CHARACTER SET utf8mb4 NULL,
    `alternative_quest_title` longtext CHARACTER SET utf8mb4 NULL,
    `alternative_quest_target` smallint unsigned NULL,
    `alternative_quest_timestamp` bigint unsigned NULL,
    `alternative_quest_reward_type` smallint unsigned AS (json_extract(json_extract(`alternative_quest_rewards`,'$[*].type'),'$[0]')),
    `alternative_quest_item_id` smallint unsigned AS (json_extract(json_extract(`alternative_quest_rewards`,'$[*].info.item_id'),'$[0]')),
    `alternative_quest_reward_amount` smallint unsigned AS (json_extract(json_extract(`alternative_quest_rewards`,'$[*].info.amount'),'$[0]')),
    `alternative_quest_pokemon_id` int unsigned AS (json_extract(json_extract(`alternative_quest_rewards`,'$[*].info.pokemon_id'),'$[0]')),
    `alternative_quest_conditions` longtext CHARACTER SET utf8mb4 NULL,
    `alternative_quest_rewards` longtext CHARACTER SET utf8mb4 NULL,
    CONSTRAINT `PK_pokestop` PRIMARY KEY (`id`)
) CHARACTER SET=utf8mb4;


CREATE TABLE `s2cell` (
    `id` bigint unsigned NOT NULL,
    `level` smallint unsigned NOT NULL,
    `center_lat` double NOT NULL,
    `center_lon` double NOT NULL,
    `updated` bigint unsigned NOT NULL,
    CONSTRAINT `PK_s2cell` PRIMARY KEY (`id`)
) CHARACTER SET=utf8mb4;


CREATE TABLE `spawnpoint` (
    `id` bigint unsigned NOT NULL,
    `lat` double NOT NULL,
    `lon` double NOT NULL,
    `despawn_sec` int unsigned NULL,
    `updated` bigint unsigned NOT NULL,
    `last_seen` bigint unsigned NULL,
    CONSTRAINT `PK_spawnpoint` PRIMARY KEY (`id`)
) CHARACTER SET=utf8mb4;


CREATE TABLE `weather` (
    `id` bigint NOT NULL,
    `level` smallint unsigned NOT NULL,
    `latitude` double NOT NULL,
    `longitude` double NOT NULL,
    `gameplay_condition` int NOT NULL,
    `wind_direction` smallint unsigned NOT NULL,
    `cloud_level` smallint unsigned NOT NULL,
    `rain_level` smallint unsigned NOT NULL,
    `wind_level` smallint unsigned NOT NULL,
    `snow_level` smallint unsigned NOT NULL,
    `fog_level` smallint unsigned NOT NULL,
    `special_effect_level` smallint unsigned NOT NULL,
    `severity` smallint unsigned NULL,
    `warn_weather` tinyint(1) NULL,
    `updated` bigint unsigned NOT NULL,
    CONSTRAINT `PK_weather` PRIMARY KEY (`id`)
) CHARACTER SET=utf8mb4;


CREATE TABLE `incident` (
    `id` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `pokestop_id` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `start` bigint unsigned NOT NULL,
    `expiration` bigint unsigned NOT NULL,
    `display_type` int unsigned NOT NULL,
    `style` int unsigned NOT NULL,
    `character` int unsigned NOT NULL,
    `updated` bigint unsigned NOT NULL,
    CONSTRAINT `PK_incident` PRIMARY KEY (`id`),
    CONSTRAINT `FK_incident_pokestop_pokestop_id` FOREIGN KEY (`pokestop_id`) REFERENCES `pokestop` (`id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;


CREATE INDEX `IX_incident_pokestop_id` ON `incident` (`pokestop_id`);