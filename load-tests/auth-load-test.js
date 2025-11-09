import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate } from 'k6/metrics';

// Custom metrics
const errorRate = new Rate('errors');

// Test configuration
export const options = {
  stages: [
    { duration: '30s', target: 10 },  // Ramp up to 10 users
    { duration: '1m', target: 50 },   // Ramp up to 50 users
    { duration: '2m', target: 100 },  // Stay at 100 users
    { duration: '1m', target: 200 },  // Peak load - 200 users
    { duration: '30s', target: 0 },   // Ramp down to 0
  ],
  thresholds: {
    http_req_duration: ['p(95)<500'], // 95% of requests should be below 500ms
    errors: ['rate<0.1'],              // Error rate should be less than 10%
  },
};

const BASE_URL = 'http://localhost:5000';

// Sample test users
const testUsers = [
  { email: 'test1@gym.com', password: 'Test123!' },
  { email: 'test2@gym.com', password: 'Test123!' },
  { email: 'test3@gym.com', password: 'Test123!' },
];

export default function () {
  // Select a random test user
  const user = testUsers[Math.floor(Math.random() * testUsers.length)];

  // Test 1: Register user (might fail if already exists - that's ok)
  const registerPayload = JSON.stringify({
    email: `user${__VU}_${Date.now()}@loadtest.com`,
    password: 'LoadTest123!',
    name: `Load Test User ${__VU}`,
    role: 'User'
  });

  const registerParams = {
    headers: { 'Content-Type': 'application/json' },
  };

  const registerRes = http.post(
    `${BASE_URL}/api/auth/register`,
    registerPayload,
    registerParams
  );

  check(registerRes, {
    'register status is 200 or 400': (r) => r.status === 200 || r.status === 400,
  }) || errorRate.add(1);

  sleep(1);

  // Test 2: Login
  const loginPayload = JSON.stringify({
    email: user.email,
    password: user.password,
  });

  const loginRes = http.post(
    `${BASE_URL}/api/auth/login`,
    loginPayload,
    registerParams
  );

  const loginSuccess = check(loginRes, {
    'login status is 200': (r) => r.status === 200,
    'login returns token': (r) => r.json('token') !== undefined,
  });

  if (!loginSuccess) {
    errorRate.add(1);
  }

  sleep(1);
}
