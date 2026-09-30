// Offer read path: list open fixtures, then read one.
import http from 'k6/http';
import { check } from 'k6';

const BASE = __ENV.BASE_URL || 'http://127.0.0.1:7100/api';

export const options = {
  scenarios: { reads: { executor: 'constant-arrival-rate', rate: Number(__ENV.RATE || 300), timeUnit: '1s', duration: __ENV.DURATION || '2m', preAllocatedVUs: 50 } },
  thresholds: { http_req_duration: ['p(99)<100'] },
};

export default function () {
  const list = http.get(`${BASE}/fixtures/?limit=20`);
  check(list, { 'listed': (r) => r.status === 200 });
  const fixtures = list.json();
  if (Array.isArray(fixtures) && fixtures.length > 0) {
    const one = http.get(`${BASE}/fixtures/${fixtures[0].fixtureId}`);
    check(one, { 'read': (r) => r.status === 200 });
  }
}
