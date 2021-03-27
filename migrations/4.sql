ALTER TABLE `instance` 
DROP KEY `fk_geofence_name`;

ALTER TABLE `instance` 
CHANGE COLUMN `geofence` `geofences` longtext NOT NULL;

UPDATE `instance` 
SET geofences = CONCAT("[\"", geofences, "\"]");
