--
-- Delete all S2 cells last updated more than 7 days ago every 24 hours
--
CREATE EVENT IF NOT EXISTS `clear_s2cell` ON SCHEDULE EVERY 24 HOUR
DO DELETE FROM s2cell WHERE updated <= UNIX_TIMESTAMP() - 604800;