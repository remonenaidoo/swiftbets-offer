SELECT s.sport_id AS SportId, s.name AS SportName, c.competition_id AS CompetitionId, c.name AS CompetitionName,
       count(f.fixture_id) FILTER (WHERE f.status = 'scheduled' AND f.kickoff_at > @Now)::int AS UpcomingFixtures
FROM catalog.sports s
JOIN catalog.competitions c ON c.sport_id = s.sport_id
LEFT JOIN catalog.fixtures f ON f.competition_id = c.competition_id
GROUP BY s.sport_id, s.name, c.competition_id, c.name
ORDER BY s.name, c.name;
