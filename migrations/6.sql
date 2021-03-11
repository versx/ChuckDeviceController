ALTER TABLE `webhook` 
DROP FOREIGN KEY `fk_webhook_geofence_name`;

ALTER TABLE `webhook` 
CHANGE `geofence` `geofences` longtext NOT NULL;

UPDATE `webhook` 
SET geofences = CONCAT("[\"", geofences, "\"]");