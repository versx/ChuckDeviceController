ALTER TABLE `instance`
ADD COLUMN `min_level` smallint(3) unsigned DEFAULT 0;

ALTER TABLE `instance`
ADD COLUMN `max_level` smallint(3) unsigned DEFAULT 29;
