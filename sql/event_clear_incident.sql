--
-- Delete all expired incidents older than 1 hour every 60 minutes
--
CREATE EVENT IF NOT EXISTS `clear_incident` ON SCHEDULE EVERY 60 MINUTE
DO DELETE FROM incident WHERE expiration <= UNIX_TIMESTAMP() - 3600;