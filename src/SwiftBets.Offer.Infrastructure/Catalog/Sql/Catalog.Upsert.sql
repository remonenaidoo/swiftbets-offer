INSERT INTO catalog.sports (sport_id, name, updated_at)
SELECT DISTINCT s.sport_id, initcap(s.sport_id), now() FROM unnest(@SportIds) AS s(sport_id)
ON CONFLICT (sport_id) DO NOTHING;

INSERT INTO catalog.competitions (competition_id, sport_id, name, updated_at)
SELECT DISTINCT ON (c.competition_id) c.competition_id, c.sport_id, c.name, now()
FROM unnest(@CompetitionIds, @SportIds, @CompetitionNames) AS c(competition_id, sport_id, name)
ON CONFLICT (competition_id) DO UPDATE SET name = EXCLUDED.name, updated_at = now()
WHERE catalog.competitions.name <> EXCLUDED.name;

INSERT INTO catalog.fixtures (fixture_id, competition_id, home_team, away_team, kickoff_at, status, offer_version, updated_at)
SELECT f.fixture_id, f.competition_id, f.home_team, f.away_team, f.kickoff_at, f.status, f.offer_version, now()
FROM unnest(@FixtureIds, @CompetitionIds, @HomeTeams, @AwayTeams, @KickoffTimes, @Statuses, @OfferVersions)
    AS f(fixture_id, competition_id, home_team, away_team, kickoff_at, status, offer_version)
ON CONFLICT (fixture_id) DO UPDATE
SET competition_id = EXCLUDED.competition_id, home_team = EXCLUDED.home_team, away_team = EXCLUDED.away_team,
    kickoff_at = EXCLUDED.kickoff_at, status = EXCLUDED.status, offer_version = EXCLUDED.offer_version, updated_at = now()
WHERE catalog.fixtures.offer_version < EXCLUDED.offer_version;
