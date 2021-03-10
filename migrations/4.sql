ALTER TABLE `instance` 
DROP FOREIGN KEY `fk_geofence_name`;

ALTER TABLE `instance` 
CHANGE `geofence` `geofences` longtext NOT NULL;
