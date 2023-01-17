DROP TRIGGER IF EXISTS invasion_inserted;

CREATE TRIGGER invasion_inserted
    AFTER INSERT ON incident
    FOR EACH ROW BEGIN
    INSERT INTO invasion_stats (grunt_type, count, date)
    VALUES (NEW.character, 1, DATE(FROM_UNIXTIME(NEW.expiration)))
    ON DUPLICATE KEY UPDATE count = count + 1;
END