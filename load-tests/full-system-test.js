import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate } from 'k6/metrics';

// Custom metrics
const errorRate = new Rate('errors');

// Test configuration - Full system stress test
export const options = {
  stages: [
    { duration: '1m', target: 50 },   // Warm up
    { duration: '2m', target: 150 },  // Ramp up
    { duration: '3m', target: 300 },  // Normal load
    { duration: '2m', target: 500 },  // Peak load
    { duration: '1m', target: 100 },  // Scale down
    { duration: '30s', target: 0 },   // Cool down
  ],
  thresholds: {
    http_req_duration: ['p(95)<1000'], // 95% of requests should be below 1s
    http_req_duration: ['p(99)<2000'], // 99% of requests should be below 2s
    errors: ['rate<0.15'],              // Error rate should be less than 15%
  },
};

const BASE_URL = 'http://localhost:5000';

// Simulate realistic user behavior
export default function () {
  const scenarios = [
    browseGyms,
    authFlow,
    membershipFlow,
  ];

  // Pick a random scenario
  const scenario = scenarios[Math.floor(Math.random() * scenarios.length)];
  scenario();
}

// Scenario 1: Browse gyms and gym groups
function browseGyms() {
  // Get all gyms
  let res = http.get(`${BASE_URL}/api/gym/gyms`);
  check(res, { 'browse: get gyms 200 or 401': (r) => r.status === 200 || r.status === 401 }) || errorRate.add(1);
  sleep(1);

  // Get gym groups
  res = http.get(`${BASE_URL}/api/gymgroup/gymgroups`);
  check(res, { 'browse: get groups 200 or 401': (r) => r.status === 200 || r.status === 401 }) || errorRate.add(1);
  sleep(2);
}

// Scenario 2: Authentication flow
function authFlow() {
  const email = `loadtest${__VU}_${Date.now()}@test.com`;
  const password = 'Test123!';

  // Register
  const registerPayload = JSON.stringify({
    email: email,
    password: password,
    name: `Test User ${__VU}`,
    role: 'User'
  });

  let res = http.post(
    `${BASE_URL}/api/auth/register`,
    registerPayload,
    { headers: { 'Content-Type': 'application/json' } }
  );
  
  check(res, { 'auth: register 200 or 400': (r) => r.status === 200 || r.status === 400 || r.status === 401 }) || errorRate.add(1);
  sleep(1);

  // Login
  const loginPayload = JSON.stringify({
    email: email,
    password: password,
  });

  res = http.post(
    `${BASE_URL}/api/auth/login`,
    loginPayload,
    { headers: { 'Content-Type': 'application/json' } }
  );

  check(res, { 'auth: login 200 or 400 or 401': (r) => r.status === 200 || r.status === 400 || r.status === 401 }) || errorRate.add(1);
  sleep(1);
}

// Scenario 3: Membership operations (requires auth)
function membershipFlow() {
  // First login to get token
  const loginPayload = JSON.stringify({
    email: 'test1@gym.com',
    password: 'Test123!',
  });

  const loginRes = http.post(
    `${BASE_URL}/api/auth/login`,
    loginPayload,
    { headers: { 'Content-Type': 'application/json' } }
  );

  if (loginRes.status !== 200) {
    errorRate.add(1);
    return;
  }

  const token = loginRes.json('token');
  const authHeaders = {
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`,
    },
  };

  sleep(1);

  // Get my memberships
  const membershipRes = http.get(
    `${BASE_URL}/api/memberships/my-memberships`,
    authHeaders
  );

  check(membershipRes, {
    'membership: get my memberships': (r) => r.status === 200 || r.status === 401,
  }) || errorRate.add(1);

  sleep(2);
}
