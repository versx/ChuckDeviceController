DROP TABLE IF EXISTS `invasion_stats`;

CREATE TABLE IF NOT EXISTS `invasion_stats` (
  `date` DATE NOT NULL,
  `grunt_type` smallint(5) unsigned NOT NULL DEFAULT 0,
  `count` int NOT NULL,
  PRIMARY KEY (`date`, `grunt_type`)
);

INSERT INTO invasion_stats (date, grunt_type, count)
SELECT DATE(FROM_UNIXTIME(expiration)) AS date, `character`, COUNT(*) AS count
FROM incident
WHERE `character` IS NOT NULL AND `character` != 0
GROUP BY `character`, date;