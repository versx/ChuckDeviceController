ALTER TABLE `webhook` 
DROP FOREIGN KEY `fk_webhook_geofence_name`;

ALTER TABLE `webhook` 
CHANGE COLUMN `geofence` `geofences` longtext DEFAULT NULL;

UPDATE `webhook` 
SET geofences = CONCAT("[\"", geofences, "\"]");
