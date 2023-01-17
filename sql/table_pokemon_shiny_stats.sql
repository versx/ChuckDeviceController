DROP TABLE IF EXISTS `pokemon_shiny_stats`;

CREATE TABLE IF NOT EXISTS `pokemon_shiny_stats` (
  `date` DATE NOT NULL,
  `pokemon_id` smallint(6) unsigned NOT NULL,
  `form_id` smallint(5) unsigned NOT NULL,
  `count` int NOT NULL,
  PRIMARY KEY (`date`, `pokemon_id`)
);

INSERT INTO pokemon_shiny_stats (date, pokemon_id, form_id, count)
SELECT DATE(FROM_UNIXTIME(expire_timestamp)) AS date, pokemon_id, form_id, COUNT(*) AS count
FROM pokemon
WHERE shiny = true
GROUP BY pokemon_id, date;