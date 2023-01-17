--
-- Delete all S2 cells last updated more than 2 hours ago every 24 hours
--
CREATE EVENT IF NOT EXISTS `clear_weather` ON SCHEDULE EVERY 24 HOUR
DO DELETE FROM weather WHERE updated <= UNIX_TIMESTAMP() - 7200;