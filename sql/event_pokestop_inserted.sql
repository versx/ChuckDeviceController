DROP TRIGGER IF EXISTS pokestop_inserted;

CREATE TRIGGER pokestop_inserted
    AFTER INSERT ON pokestop
    FOR EACH ROW BEGIN
    IF (NEW.quest_type IS NOT NULL AND NEW.quest_type != 0) THEN
    INSERT INTO quest_stats (reward_type, pokemon_id, item_id, count, date)
    VALUES (NEW.quest_reward_type, IFNULL(NEW.quest_pokemon_id, 0), IFNULL(NEW.quest_item_id, 0), 1, DATE(FROM_UNIXTIME(NEW.quest_timestamp)))
    ON DUPLICATE KEY UPDATE count = count + 1;
    END IF;

    IF (NEW.alternative_quest_type IS NOT NULL AND NEW.alternative_quest_type != 0) THEN
    INSERT INTO quest_stats (reward_type, pokemon_id, item_id, count, date)
    VALUES (NEW.alternative_quest_reward_type, IFNULL(NEW.alternative_quest_pokemon_id, 0), IFNULL(NEW.alternative_quest_item_id, 0), 1, DATE(FROM_UNIXTIME(NEW.alternative_quest_timestamp)))
    ON DUPLICATE KEY UPDATE count = count + 1;
    END IF;
END