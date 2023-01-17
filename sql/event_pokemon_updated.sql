DROP TRIGGER IF EXISTS pokemon_updated;

CREATE TRIGGER pokemon_updated
    BEFORE UPDATE ON pokemon
    FOR EACH ROW BEGIN
    IF (NEW.iv IS NOT NULL AND OLD.iv IS NULL) THEN BEGIN
        INSERT INTO pokemon_iv_stats (pokemon_id, count, date)
        VALUES
            (NEW.pokemon_id, 1, DATE(FROM_UNIXTIME(NEW.expire_timestamp)))
        ON DUPLICATE KEY UPDATE
            count = count + 1;
        END;
    END IF;
    IF (NEW.shiny = 1 AND (OLD.shiny = 0 OR OLD.shiny IS NULL)) THEN BEGIN
        INSERT INTO pokemon_shiny_stats (pokemon_id, count, date)
        VALUES
            (NEW.pokemon_id, 1, DATE(FROM_UNIXTIME(NEW.expire_timestamp)))
        ON DUPLICATE KEY UPDATE
            count = count + 1;
        END;
    END IF;
    IF (NEW.iv = 100 AND OLD.iv IS NULL) THEN BEGIN
        INSERT INTO pokemon_hundo_stats (pokemon_id, count, date)
        VALUES
            (NEW.pokemon_id, 1, DATE(FROM_UNIXTIME(NEW.expire_timestamp)))
        ON DUPLICATE KEY UPDATE
            count = count + 1;
        END;
    END IF;
END