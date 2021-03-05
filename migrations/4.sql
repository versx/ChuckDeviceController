CREATE TABLE `device_group` (
   `name` varchar(30) NOT NULL,
   `device_uuids` longtext NOT NULL,
   PRIMARY KEY (`name`),
   UNIQUE KEY `name` (`name`)
);

DROP TABLE IF EXISTS `assignment`;
CREATE TABLE `assignment` (
   `id` int(10) unsigned NOT NULL AUTO_INCREMENT,
   `device_uuid` varchar(40) DEFAULT NULL,
   `device_group_name` varchar(30) DEFAULT NULL,
   `instance_name` varchar(30) NOT NULL,
   `source_instance_name` varchar(30) DEFAULT NULL,
   `time` mediumint(6) unsigned NOT NULL,
   `date` date DEFAULT NULL,
   `enabled` tinyint(1) unsigned NOT NULL DEFAULT 1,
   PRIMARY KEY (`id`),
   UNIQUE KEY assignment_unique (`device_uuid`,`device_group_name`,`instance_name`,`time`,`date`),
   KEY `assignment_fk_instance_name` (`instance_name`),
   KEY `assignment_fk_source_instance_name` (`source_instance_name`),
   CONSTRAINT `assignment_fk_instance_name` FOREIGN KEY (`instance_name`) REFERENCES `instance` (`name`) ON DELETE CASCADE ON UPDATE CASCADE,
   CONSTRAINT `assignment_fk_source_instance_name` FOREIGN KEY (`source_instance_name`) REFERENCES `instance` (`name`) ON DELETE CASCADE ON UPDATE CASCADE,
   CONSTRAINT `assignment_fk_device_uuid` FOREIGN KEY (`device_uuid`) REFERENCES `device`(`uuid`) ON DELETE CASCADE ON UPDATE CASCADE,
   CONSTRAINT `assignment_fk_source_device_group_name` FOREIGN KEY (`device_group_name`) REFERENCES `device_group`(`name`) ON DELETE CASCADE ON UPDATE CASCADE
);