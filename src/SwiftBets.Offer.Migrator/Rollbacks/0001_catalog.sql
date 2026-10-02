-- Rolls back 0001_catalog. The catalogue is rebuilt from the feed, so nothing is lost for good.
DROP SCHEMA IF EXISTS catalog CASCADE;
