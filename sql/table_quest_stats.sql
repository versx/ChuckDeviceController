DROP TABLE IF EXISTS `quest_stats`;

CREATE TABLE IF NOT EXISTS `quest_stats` (
  `date` DATE NOT NULL,
  `reward_type` smallint(6) unsigned NOT NULL DEFAULT 0,
  `pokemon_id` smallint(6) unsigned NOT NULL DEFAULT 0,
  `item_id` smallint(6) unsigned NOT NULL DEFAULT 0,
  `is_alternative` tinyint(1) unsigned NOT NULL DEFAULT 0,
  `count` int NOT NULL,
  PRIMARY KEY (`date`, `reward_type`, `pokemon_id`, `item_id`, `is_alternative`)
);

INSERT INTO quest_stats (date, reward_type, pokemon_id, item_id, is_alternative, count)
SELECT DATE(FROM_UNIXTIME(quest_timestamp)) AS date, quest_reward_type, COALESCE(quest_pokemon_id, 0), COALESCE(quest_item_id, 0), 0 AS is_alternative, COUNT(*) AS count
FROM pokestop
WHERE quest_type IS NOT NULL
GROUP BY quest_reward_type, quest_pokemon_id, quest_item_id, date;

INSERT INTO quest_stats (date, reward_type, pokemon_id, item_id, is_alternative, count)
SELECT DATE(FROM_UNIXTIME(alternative_quest_timestamp)) AS date, alternative_quest_reward_type, COALESCE(quest_pokemon_id, 0), COALESCE(alternative_quest_item_id, 0), 1 AS is_alternative, COUNT(*) AS count
FROM pokestop
WHERE alternative_quest_type IS NOT NULL
GROUP BY alternative_quest_reward_type, alternative_quest_pokemon_id, alternative_quest_item_id, date;