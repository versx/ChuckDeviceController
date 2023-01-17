DROP TRIGGER IF EXISTS invasion_updated;

DELIMITER /

CREATE TRIGGER invasion_updated
    BEFORE UPDATE ON incident
    FOR EACH ROW BEGIN
    IF (NEW.`character` != OLD.`character`) THEN
        INSERT INTO invasion_stats (grunt_type, count, date)
        VALUES
            (NEW.character, 1, DATE(FROM_UNIXTIME(NEW.expiration)))
        ON DUPLICATE KEY UPDATE
            count = count + 1;
    END IF;
END/

DELIMITER ;