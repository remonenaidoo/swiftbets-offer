-- KEYS[1] fixture hash, KEYS[2] open-fixtures sorted set
-- ARGV: expected version, new version, snapshot json, is-open flag, kickoff ms, ttl seconds, fixture id
local stored = redis.call('HGET', KEYS[1], 'version') or '0'
if stored ~= ARGV[1] then
  return 0
end
redis.call('HSET', KEYS[1], 'version', ARGV[2], 'snapshot', ARGV[3])
redis.call('EXPIRE', KEYS[1], tonumber(ARGV[6]))
if ARGV[4] == '1' then
  redis.call('ZADD', KEYS[2], tonumber(ARGV[5]), ARGV[7])
else
  redis.call('ZREM', KEYS[2], ARGV[7])
end
return 1
