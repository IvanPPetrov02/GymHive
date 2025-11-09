import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate } from 'k6/metrics';

// Custom metrics
const errorRate = new Rate('errors');

// Test configuration
export const options = {
  stages: [
    { duration: '30s', target: 20 },  // Ramp up to 20 users
    { duration: '1m', target: 100 },  // Ramp up to 100 users
    { duration: '2m', target: 200 },  // Stay at 200 users
    { duration: '1m', target: 300 },  // Peak load - 300 users
    { duration: '30s', target: 0 },   // Ramp down to 0
  ],
  thresholds: {
    http_req_duration: ['p(95)<500'], // 95% of requests should be below 500ms
    errors: ['rate<0.1'],              // Error rate should be less than 10%
  },
};

const BASE_URL = 'http://localhost:5000';

export default function () {
  // Test 1: Get all gyms
  const gymsRes = http.get(`${BASE_URL}/api/gym/gyms`);
  
  check(gymsRes, {
    'get gyms status is 200 or 401': (r) => r.status === 200 || r.status === 401,
  }) || errorRate.add(1);

  sleep(1);

  // Test 2: Get gym groups
  const groupsRes = http.get(`${BASE_URL}/api/gymgroup/gymgroups`);
  
  check(groupsRes, {
    'get gym groups status is 200 or 401': (r) => r.status === 200 || r.status === 401,
  }) || errorRate.add(1);

  sleep(1);

  // Test 3: Try to get a specific gym (ID 1)
  const gymRes = http.get(`${BASE_URL}/api/gym/gyms/1`);
  
  check(gymRes, {
    'get gym by id status is 200, 401, or 404': (r) => r.status === 200 || r.status === 401 || r.status === 404,
  }) || errorRate.add(1);

  sleep(2);
}
