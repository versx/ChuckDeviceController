DROP TRIGGER IF EXISTS gym_updated;

CREATE TRIGGER gym_updated
BEFORE UPDATE ON gym
FOR EACH ROW BEGIN
  IF ((OLD.raid_pokemon_id IS NULL OR OLD.raid_pokemon_id = 0) AND (NEW.raid_pokemon_id IS NOT NULL AND NEW.raid_pokemon_id != 0)) THEN
    INSERT INTO raid_stats (pokemon_id, level, count, date)
    VALUES
      (NEW.raid_pokemon_id, NEW.raid_level, 1, DATE(FROM_UNIXTIME(NEW.raid_end_timestamp)))
    ON DUPLICATE KEY UPDATE
      count = count + 1;
  END IF;
END