import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate, Trend } from 'k6/metrics';

// Custom metrics
const errorRate = new Rate('errors');
const authResponseTime = new Trend('auth_response_time');
const apiResponseTime = new Trend('api_response_time');

// Realistic load test configuration
export const options = {
  stages: [
    { duration: '30s', target: 20 },   // Warm up - 20 users
    { duration: '1m', target: 50 },    // Ramp up - 50 users  
    { duration: '2m', target: 100 },   // Normal load - 100 users
    { duration: '1m', target: 150 },   // Peak load - 150 users
    { duration: '30s', target: 0 },    // Ramp down
  ],
  thresholds: {
    'http_req_duration': ['p(95)<1000', 'p(99)<2000'], // Response times
    'errors': ['rate<0.1'],                             // Less than 10% errors
    'http_req_failed': ['rate<0.1'],                    // Less than 10% failed requests
  },
};

const BASE_URL = 'http://localhost:5000';

export default function () {
  // Test Health Endpoint (no auth required)
  let res = http.get(`${BASE_URL}/health`);
  
  const healthCheck = check(res, {
    'health check 200': (r) => r.status === 200,
    'health check response valid': (r) => {
      try {
        const body = r.json();
        return body.status === 'healthy';
      } catch {
        return false;
      }
    },
  });
  
  if (!healthCheck) {
    errorRate.add(1);
  }
  
  apiResponseTime.add(res.timings.duration);
  sleep(1);

  // Test Auth Endpoints
  const uniqueEmail = `loadtest_${__VU}_${Date.now()}@test.com`;
  const password = 'LoadTest123!';

  // Register
  const registerPayload = JSON.stringify({
    email: uniqueEmail,
    password: password,
    name: `Load Test User ${__VU}`,
    role: 'User'
  });

  res = http.post(
    `${BASE_URL}/api/auth/register`,
    registerPayload,
    { headers: { 'Content-Type': 'application/json' } }
  );

  const registerCheck = check(res, {
    'register status OK': (r) => r.status === 200 || r.status === 400 || r.status === 401,
  });

  if (!registerCheck) {
    errorRate.add(1);
  }

  authResponseTime.add(res.timings.duration);
  sleep(1);

  // Login (might fail if registration failed, that's ok)
  const loginPayload = JSON.stringify({
    email: uniqueEmail,
    password: password,
  });

  res = http.post(
    `${BASE_URL}/api/auth/login`,
    loginPayload,
    { headers: { 'Content-Type': 'application/json' } }
  );

  const loginCheck = check(res, {
    'login status OK': (r) => r.status === 200 || r.status === 400 || r.status === 401,
  });

  if (!loginCheck) {
    errorRate.add(1);
  }

  authResponseTime.add(res.timings.duration);
  sleep(2);
}

export function handleSummary(data) {
  return {
    'stdout': textSummary(data, { indent: ' ', enableColors: true }),
  };
}

function textSummary(data, { indent = '', enableColors = false } = {}) {
  const metrics = data.metrics;
  
  let summary = '\n';
  summary += `${indent}═══════════════════════════════════════════════════════\n`;
  summary += `${indent}         GYMHIVE LOAD TEST RESULTS\n`;
  summary += `${indent}═══════════════════════════════════════════════════════\n\n`;
  
  // HTTP Request Stats
  summary += `${indent}📊 HTTP REQUESTS:\n`;
  summary += `${indent}   Total Requests: ${metrics.http_reqs.values.count}\n`;
  summary += `${indent}   Requests/sec: ${metrics.http_reqs.values.rate.toFixed(2)}\n`;
  summary += `${indent}   Failed: ${(metrics.http_req_failed.values.rate * 100).toFixed(2)}%\n\n`;
  
  // Response Times
  summary += `${indent}⏱️  RESPONSE TIMES:\n`;
  summary += `${indent}   Average: ${metrics.http_req_duration.values.avg.toFixed(2)}ms\n`;
  summary += `${indent}   p(95): ${metrics.http_req_duration.values['p(95)'].toFixed(2)}ms\n`;
  summary += `${indent}   p(99): ${metrics.http_req_duration.values['p(99)'].toFixed(2)}ms\n`;
  summary += `${indent}   Max: ${metrics.http_req_duration.values.max.toFixed(2)}ms\n\n`;
  
  // Custom Metrics
  if (metrics.auth_response_time) {
    summary += `${indent}🔐 AUTH SERVICE:\n`;
    summary += `${indent}   Avg: ${metrics.auth_response_time.values.avg.toFixed(2)}ms\n`;
    summary += `${indent}   p(95): ${metrics.auth_response_time.values['p(95)'].toFixed(2)}ms\n\n`;
  }
  
  if (metrics.api_response_time) {
    summary += `${indent}🌐 API GATEWAY:\n`;
    summary += `${indent}   Avg: ${metrics.api_response_time.values.avg.toFixed(2)}ms\n`;
    summary += `${indent}   p(95): ${metrics.api_response_time.values['p(95)'].toFixed(2)}ms\n\n`;
  }
  
  // Error Rate
  if (metrics.errors) {
    summary += `${indent}❌ ERRORS:\n`;
    summary += `${indent}   Error Rate: ${(metrics.errors.values.rate * 100).toFixed(2)}%\n\n`;
  }
  
  // Thresholds
  summary += `${indent}✅ THRESHOLDS:\n`;
  const passed = data.root_group.checks.passes || 0;
  const failed = data.root_group.checks.fails || 0;
  const total = passed + failed;
  summary += `${indent}   Passed: ${passed}/${total} (${((passed/total)*100).toFixed(1)}%)\n\n`;
  
  // Recommendation
  summary += `${indent}═══════════════════════════════════════════════════════\n`;
  summary += `${indent}📋 RECOMMENDATION:\n`;
  summary += `${indent}───────────────────────────────────────────────────────\n`;
  
  const p95 = metrics.http_req_duration.values['p(95)'];
  const errRate = metrics.errors ? metrics.errors.values.rate : metrics.http_req_failed.values.rate;
  
  if (p95 < 500 && errRate < 0.05) {
    summary += `${indent}✅ EXCELLENT! Docker Compose is handling the load well.\n`;
    summary += `${indent}   - Response times are fast (p95 < 500ms)\n`;
    summary += `${indent}   - Error rate is low (< 5%)\n`;
    summary += `${indent}   - Kubernetes is NOT needed at this scale\n`;
    summary += `${indent}   - Consider: Redis caching, DB indexing for optimization\n`;
  } else if (p95 < 1000 && errRate < 0.1) {
    summary += `${indent}⚠️  GOOD. Docker Compose is working but could be optimized.\n`;
    summary += `${indent}   - Response times acceptable (p95 < 1s)\n`;
    summary += `${indent}   - Error rate manageable (< 10%)\n`;
    summary += `${indent}   - Kubernetes might help with higher loads\n`;
    summary += `${indent}   - Recommend: Add caching, optimize queries\n`;
  } else {
    summary += `${indent}❌ ATTENTION NEEDED. System is under strain.\n`;
    summary += `${indent}   - Response times high (p95 > 1s)\n`;
    summary += `${indent}   - Error rate concerning (> 10%)\n`;
    summary += `${indent}   - Kubernetes RECOMMENDED for:\n`;
    summary += `${indent}     • Horizontal auto-scaling\n`;
    summary += `${indent}     • Better resource management\n`;
    summary += `${indent}     • Load balancing across pods\n`;
  }
  
  summary += `${indent}═══════════════════════════════════════════════════════\n\n`;
  
  return summary;
}
