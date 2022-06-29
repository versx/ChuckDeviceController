ALTER DATABASE CHARACTER SET utf8mb4;


CREATE TABLE `account` (
    `username` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `password` longtext CHARACTER SET utf8mb4 NOT NULL,
    `first_warning_timestamp` bigint unsigned NULL,
    `failed_timestamp` bigint unsigned NULL,
    `failed` longtext CHARACTER SET utf8mb4 NULL,
    `level` smallint unsigned NOT NULL,
    `last_encounter_time` bigint unsigned NULL,
    `last_encounter_lat` double NULL,
    `last_encounter_lon` double NULL,
    `spins` int unsigned NULL,
    `tutorial` smallint unsigned NULL,
    `creation_timestamp` bigint unsigned NULL,
    `warn` tinyint(1) NULL,
    `warn_expire_timestamp` bigint unsigned NULL,
    `warn_message_acknowledged` tinyint(1) NULL,
    `suspended_message_acknowledged` tinyint(1) NULL,
    `was_suspended` tinyint(1) NULL,
    `banned` tinyint(1) NULL,
    `last_used_timestamp` bigint unsigned NULL,
    `group` longtext CHARACTER SET utf8mb4 NULL,
    CONSTRAINT `PK_account` PRIMARY KEY (`username`)
) CHARACTER SET=utf8mb4;


CREATE TABLE `assignment` (
    `id` int unsigned NOT NULL AUTO_INCREMENT,
    `instance_name` longtext CHARACTER SET utf8mb4 NOT NULL,
    `source_instance_name` longtext CHARACTER SET utf8mb4 NOT NULL,
    `device_uuid` longtext CHARACTER SET utf8mb4 NOT NULL,
    `time` int unsigned NOT NULL,
    `date` datetime(6) NULL,
    `device_group_name` longtext CHARACTER SET utf8mb4 NOT NULL,
    `enabled` tinyint(1) NOT NULL,
    CONSTRAINT `PK_assignment` PRIMARY KEY (`id`)
) CHARACTER SET=utf8mb4;


CREATE TABLE `device` (
    `uuid` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `instance_name` longtext CHARACTER SET utf8mb4 NOT NULL,
    `account_username` longtext CHARACTER SET utf8mb4 NOT NULL,
    `last_host` longtext CHARACTER SET utf8mb4 NOT NULL,
    `last_lat` double NULL,
    `last_lon` double NULL,
    `last_seen` bigint unsigned NULL,
    CONSTRAINT `PK_device` PRIMARY KEY (`uuid`)
) CHARACTER SET=utf8mb4;


CREATE TABLE `geofence` (
    `name` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `type` longtext CHARACTER SET utf8mb4 NOT NULL,
    `data` longtext CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK_geofence` PRIMARY KEY (`name`)
) CHARACTER SET=utf8mb4;


CREATE TABLE `instance` (
    `name` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `type` longtext CHARACTER SET utf8mb4 NOT NULL,
    `min_level` smallint unsigned NOT NULL,
    `max_level` smallint unsigned NOT NULL,
    `geofences` longtext CHARACTER SET utf8mb4 NOT NULL,
    `data` longtext CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK_instance` PRIMARY KEY (`name`)
) CHARACTER SET=utf8mb4;


CREATE TABLE `iv_list` (
    `name` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `pokemon_ids` longtext CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK_iv_list` PRIMARY KEY (`name`)
) CHARACTER SET=utf8mb4;


CREATE TABLE `webhook` (
    `name` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `types` longtext CHARACTER SET utf8mb4 NOT NULL,
    `delay` double NOT NULL,
    `url` longtext CHARACTER SET utf8mb4 NOT NULL,
    `enabled` tinyint(1) NOT NULL,
    `geofences` longtext CHARACTER SET utf8mb4 NOT NULL,
    `data` longtext CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK_webhook` PRIMARY KEY (`name`)
) CHARACTER SET=utf8mb4;