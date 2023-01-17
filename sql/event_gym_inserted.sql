DROP TRIGGER IF EXISTS gym_inserted;

DELIMITER /

CREATE TRIGGER gym_inserted
    AFTER INSERT ON gym
    FOR EACH ROW BEGIN
    IF (NEW.raid_pokemon_id IS NOT NULL AND NEW.raid_pokemon_id != 0) THEN
        INSERT INTO raid_stats (pokemon_id, form_id, level, count, date)
        VALUES
            (NEW.raid_pokemon_id, NEW.raid_pokemon_form, NEW.raid_level, 1, DATE(FROM_UNIXTIME(NEW.raid_end_timestamp)))
        ON DUPLICATE KEY UPDATE
            count = count + 1;
    END IF;
END/

DELIMITER ;