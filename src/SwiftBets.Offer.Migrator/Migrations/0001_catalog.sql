CREATE SCHEMA IF NOT EXISTS catalog;

CREATE TABLE catalog.sports
(
    sport_id   text        PRIMARY KEY,
    name       text        NOT NULL,
    updated_at timestamptz NOT NULL
);

CREATE TABLE catalog.competitions
(
    competition_id text        PRIMARY KEY,
    sport_id       text        NOT NULL REFERENCES catalog.sports (sport_id),
    name           text        NOT NULL,
    updated_at     timestamptz NOT NULL
);

CREATE TABLE catalog.fixtures
(
    fixture_id     text        PRIMARY KEY,
    competition_id text        NOT NULL REFERENCES catalog.competitions (competition_id),
    home_team      text        NOT NULL,
    away_team      text        NOT NULL,
    kickoff_at     timestamptz NOT NULL,
    status         text        NOT NULL,
    offer_version  bigint      NOT NULL,
    updated_at     timestamptz NOT NULL
);

CREATE INDEX ix_catalog_fixtures_competition ON catalog.fixtures (competition_id, kickoff_at);
CREATE INDEX ix_catalog_fixtures_kickoff ON catalog.fixtures (kickoff_at);
