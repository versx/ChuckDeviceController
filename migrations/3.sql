CREATE TABLE `webhook` (
   `name` varchar(30) NOT NULL,
   `url` varchar(255) NOT NULL,
   `delay` double DEFAULT 5,
   `types` longtext DEFAULT NULL,
   `geofence` varchar(255) DEFAULT NULL,
   `enabled` tinyint(1) unsigned DEFAULT 1,
   PRIMARY KEY `name` (`name`),
   CONSTRAINT `fk_webhook_geofence_name` FOREIGN KEY (`geofence`) REFERENCES `geofence` (`name`) ON DELETE SET NULL ON UPDATE CASCADE
);