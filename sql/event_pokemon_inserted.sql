DROP TRIGGER IF EXISTS pokemon_inserted;

DELIMITER /

CREATE TRIGGER pokemon_inserted
    BEFORE INSERT ON pokemon
    FOR EACH ROW BEGIN
    INSERT INTO pokemon_stats (pokemon_id, form_id, count, date)
    VALUES
        (NEW.pokemon_id, NEW.form, 1, DATE(FROM_UNIXTIME(NEW.expire_timestamp)))
    ON DUPLICATE KEY UPDATE
        count = count + 1;
    IF (NEW.iv IS NOT NULL) THEN BEGIN
        INSERT INTO pokemon_iv_stats (pokemon_id, form_id, iv, count, date)
        VALUES
            (NEW.pokemon_id, NEW.form, NEW.iv, 1, DATE(FROM_UNIXTIME(NEW.expire_timestamp)))
        ON DUPLICATE KEY UPDATE
            count = count + 1;
        END;
    END IF;
    IF (NEW.shiny = 1) THEN BEGIN
        INSERT INTO pokemon_shiny_stats (pokemon_id, form_id, count, date)
        VALUES
            (NEW.pokemon_id, NEW.form, 1, DATE(FROM_UNIXTIME(NEW.expire_timestamp)))
        ON DUPLICATE KEY UPDATE
            count = count + 1;
        END;
    END IF;
    IF (NEW.iv = 100) THEN BEGIN
        INSERT INTO pokemon_hundo_stats (pokemon_id, form_id, count, date)
        VALUES
            (NEW.pokemon_id, NEW.form, 1, DATE(FROM_UNIXTIME(NEW.expire_timestamp)))
        ON DUPLICATE KEY UPDATE
            count = count + 1;
        END;
    END IF;
END/

DELIMITER ;