DROP TABLE IF EXISTS `raid_stats`;

CREATE TABLE IF NOT EXISTS `raid_stats` (
  `date` date NOT NULL,
  `pokemon_id` smallint(6) unsigned NOT NULL,
  `form_id` smallint(5) unsigned NOT NULL,
  `level` smallint(3) unsigned DEFAULT NULL,
  `count` int(11) NOT NULL,
  PRIMARY KEY (`date`, `pokemon_id`, `form_id`)
);

INSERT INTO raid_stats (date, pokemon_id, form_id, level, count)
SELECT DATE(FROM_UNIXTIME(raid_end_timestamp)) AS date, raid_pokemon_id, raid_pokemon_form, raid_level, COUNT(*) AS count
FROM gym
WHERE raid_end_timestamp IS NOT NULL AND raid_pokemon_id IS NOT NULL
GROUP BY raid_pokemon_id, raid_pokemon_form, date;