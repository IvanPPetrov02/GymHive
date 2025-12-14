import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate, Trend, Counter } from 'k6/metrics';

// Custom metrics
const errorRate = new Rate('error_rate');
const gymListDuration = new Trend('gym_list_duration');
const gymDetailsDuration = new Trend('gym_details_duration');
const registerDuration = new Trend('register_duration');
const loginDuration = new Trend('login_duration');
const successfulRequests = new Counter('successful_requests');

// Configuration
const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';
const ENVIRONMENT = __ENV.ENVIRONMENT || 'docker';

// Test configurations for different environments
const configs = {
  docker: {
    stages: [
      { duration: '1m', target: 50 },     // Warm up
      { duration: '2m', target: 100 },    // Normal load
      { duration: '2m', target: 150 },    // Peak load
      { duration: '1m', target: 0 },      // Cool down
    ],
    thresholds: {
      http_req_duration: ['p(95)<2500'],
      error_rate: ['rate<0.10'],
    },
  },
  kubernetes: {
    stages: [
      { duration: '1m', target: 50 },     // Warm up
      { duration: '2m', target: 100 },    // Normal load
      { duration: '2m', target: 150 },    // Peak load (same as Docker)
      { duration: '1m', target: 0 },      // Cool down
    ],
    thresholds: {
      http_req_duration: ['p(95)<2500'],  // Same as Docker
      error_rate: ['rate<0.10'],          // Same as Docker
    },
  },
  hpa: {
    stages: [
      { duration: '2m', target: 200 },    // Initial load
      { duration: '3m', target: 500 },    // Trigger HPA
      { duration: '5m', target: 1000 },   // Sustained high load
      { duration: '3m', target: 1500 },   // Peak load
      { duration: '5m', target: 1500 },   // Sustained peak (observe scaling)
      { duration: '3m', target: 300 },    // Scale down
      { duration: '2m', target: 0 },      // Cool down
    ],
    thresholds: {
      http_req_duration: ['p(95)<1000'],
      error_rate: ['rate<0.02'],
    },
  },
};

export const options = configs[ENVIRONMENT];

let authToken = null;

export default function () {
  const scenario = Math.floor(Math.random() * 5);

  switch (scenario) {
    case 0:
      browseGymsScenario();
      break;
    case 1:
      authenticationScenario();
      break;
    case 2:
      gymDetailsScenario();
      break;
    case 3:
      fullUserJourneyScenario();
      break;
    case 4:
      membershipScenario();
      break;
  }

  sleep(Math.random() * 2 + 1); // Random sleep 1-3 seconds
}

function browseGymsScenario() {
  // Anonymous user browsing gyms (will get 401 but that's expected)
  const params = {
    headers: { 'Accept': 'application/json' },
    tags: { scenario: 'browse', endpoint: 'gyms' },
  };

  const startTime = Date.now();
  const res = http.get(`${BASE_URL}/api/gyms`, params);
  const duration = Date.now() - startTime;

  const success = check(res, {
    'browse: status 200 or 401': (r) => r.status === 200 || r.status === 401,
  });

  if (success) {
    successfulRequests.add(1);
    if (res.status === 200) {
      gymListDuration.add(duration);
    }
  } else {
    errorRate.add(1);
  }
}

function gymDetailsScenario() {
  // Try to get gym details
  const gymId = Math.floor(Math.random() * 10) + 1;
  
  const params = {
    headers: { 'Accept': 'application/json' },
    tags: { scenario: 'gym_details', endpoint: 'gym' },
  };

  const startTime = Date.now();
  const res = http.get(`${BASE_URL}/api/gyms/${gymId}`, params);
  const duration = Date.now() - startTime;

  const success = check(res, {
    'gym_details: status 200, 401, or 404': (r) => 
      r.status === 200 || r.status === 401 || r.status === 404,
  });

  if (success) {
    successfulRequests.add(1);
    if (res.status === 200) {
      gymDetailsDuration.add(duration);
    }
  } else {
    errorRate.add(1);
  }
}

function authenticationScenario() {
  const timestamp = Date.now();
  const vuId = __VU;
  const uniqueEmail = `user_${vuId}_${timestamp}@loadtest.com`;

  // Register
  const registerPayload = JSON.stringify({
    email: uniqueEmail,
    password: 'Test123!',
    name: `User${vuId}`,
    surname: `Test`,
    role: 'User'
  });

  const params = {
    headers: { 'Content-Type': 'application/json' },
    tags: { scenario: 'auth', endpoint: 'register' },
  };

  const registerStart = Date.now();
  const registerRes = http.post(
    `${BASE_URL}/api/auth/register`,
    registerPayload,
    params
  );
  const registerDur = Date.now() - registerStart;

  const registerSuccess = check(registerRes, {
    'auth: register status 200 or 400': (r) => r.status === 200 || r.status === 400,
  });

  if (registerSuccess) {
    successfulRequests.add(1);
    if (registerRes.status === 200) {
      registerDuration.add(registerDur);
    }
  } else {
    errorRate.add(1);
  }

  sleep(0.5);

  // Login
  const loginPayload = JSON.stringify({
    email: uniqueEmail,
    password: 'Test123!',
  });

  const loginParams = {
    headers: { 'Content-Type': 'application/json' },
    tags: { scenario: 'auth', endpoint: 'login' },
  };

  const loginStart = Date.now();
  const loginRes = http.post(
    `${BASE_URL}/api/auth/login`,
    loginPayload,
    loginParams
  );
  const loginDur = Date.now() - loginStart;

  const loginSuccess = check(loginRes, {
    'auth: login status 200 or 400': (r) => r.status === 200 || r.status === 400 || r.status === 401,
    'auth: login returns token (if 200)': (r) => {
      if (r.status !== 200) return true;
      try {
        return r.json('token') !== undefined;
      } catch (e) {
        return false;
      }
    },
  });

  if (loginSuccess) {
    successfulRequests.add(1);
    if (loginRes.status === 200) {
      loginDuration.add(loginDur);
      try {
        authToken = loginRes.json('token');
      } catch (e) {
        // Token parsing failed
      }
    }
  } else {
    errorRate.add(1);
  }
}

function fullUserJourneyScenario() {
  // Complete user journey: Register -> Login -> Browse gyms
  const timestamp = Date.now();
  const vuId = __VU;
  const uniqueEmail = `journey_${vuId}_${timestamp}@loadtest.com`;

  // Step 1: Register
  const registerPayload = JSON.stringify({
    email: uniqueEmail,
    password: 'Journey123!',
    name: `Journey${vuId}`,
    surname: `User`,
    role: 'User'
  });

  const registerRes = http.post(
    `${BASE_URL}/api/auth/register`,
    registerPayload,
    { headers: { 'Content-Type': 'application/json' }, tags: { scenario: 'journey' } }
  );

  if (registerRes.status !== 200) {
    errorRate.add(1);
    return;
  }

  successfulRequests.add(1);
  sleep(0.3);

  // Step 2: Login
  const loginPayload = JSON.stringify({
    email: uniqueEmail,
    password: 'Journey123!',
  });

  const loginRes = http.post(
    `${BASE_URL}/api/auth/login`,
    loginPayload,
    { headers: { 'Content-Type': 'application/json' }, tags: { scenario: 'journey' } }
  );

  if (loginRes.status !== 200) {
    errorRate.add(1);
    return;
  }

  let token;
  try {
    token = loginRes.json('token');
    successfulRequests.add(1);
  } catch (e) {
    errorRate.add(1);
    return;
  }

  sleep(0.5);

  // Step 3: Browse gyms with authentication
  const gymsRes = http.get(
    `${BASE_URL}/api/gyms`,
    { 
      headers: { 
        'Authorization': `Bearer ${token}`,
        'Accept': 'application/json' 
      },
      tags: { scenario: 'journey' }
    }
  );

  if (gymsRes.status === 200 || gymsRes.status === 401) {
    successfulRequests.add(1);
  } else {
    errorRate.add(1);
  }
}

// Membership scenario: Register, Login, Create membership, View my memberships
function membershipScenario() {
  const timestamp = Date.now();
  const vuId = __VU;
  const uniqueEmail = `membership_${vuId}_${timestamp}@loadtest.com`;

  // Step 1: Register
  const registerPayload = JSON.stringify({
    email: uniqueEmail,
    password: 'Member123!',
    name: `Member${vuId}`,
    surname: `User`,
    role: 'User'
  });

  const registerRes = http.post(
    `${BASE_URL}/api/auth/register`,
    registerPayload,
    { headers: { 'Content-Type': 'application/json' }, tags: { scenario: 'membership', endpoint: 'register' } }
  );

  if (registerRes.status !== 200) {
    errorRate.add(1);
    return;
  }

  successfulRequests.add(1);
  sleep(0.3);

  // Step 2: Login
  const loginPayload = JSON.stringify({
    email: uniqueEmail,
    password: 'Member123!',
  });

  const loginRes = http.post(
    `${BASE_URL}/api/auth/login`,
    loginPayload,
    { headers: { 'Content-Type': 'application/json' }, tags: { scenario: 'membership', endpoint: 'login' } }
  );

  if (loginRes.status !== 200) {
    errorRate.add(1);
    return;
  }

  let token;
  try {
    token = loginRes.json('token');
    successfulRequests.add(1);
  } catch (e) {
    errorRate.add(1);
    return;
  }

  sleep(0.5);

  // Step 3: Create a membership
  const gymId = Math.floor(Math.random() * 10) + 1;
  const createMembershipPayload = JSON.stringify({
    gymId: gymId,
    startDate: new Date().toISOString(),
    endDate: new Date(Date.now() + 30 * 24 * 60 * 60 * 1000).toISOString(), // 30 days from now
    membershipType: 'Monthly'
  });

  const createMembershipRes = http.post(
    `${BASE_URL}/api/memberships`,
    createMembershipPayload,
    { 
      headers: { 
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json' 
      },
      tags: { scenario: 'membership', endpoint: 'create-membership' }
    }
  );

  if (createMembershipRes.status === 200 || createMembershipRes.status === 201) {
    successfulRequests.add(1);
  } else {
    errorRate.add(1);
  }

  sleep(0.5);

  // Step 4: View my memberships
  const myMembershipsRes = http.get(
    `${BASE_URL}/api/memberships/my-memberships`,
    { 
      headers: { 
        'Authorization': `Bearer ${token}`,
        'Accept': 'application/json' 
      },
      tags: { scenario: 'membership', endpoint: 'my-memberships' }
    }
  );

  if (myMembershipsRes.status === 200) {
    successfulRequests.add(1);
  } else {
    errorRate.add(1);
  }
}

export function handleSummary(data) {
  const timestamp = new Date().toISOString().replace(/[:.]/g, '-').slice(0, -5);
  const testName = 'full-system-test';
  const env = ENVIRONMENT;
  const jsonFilename = `../results/${env}-${testName}-${timestamp}.json`;
  const mdFilename = `../results/${env}-${testName}-${timestamp}.md`;

  console.log(`\n📊 Saving results to: ${mdFilename}\n`);

  try {
    const textSummary = generateTextSummary(data);
    const markdownReport = generateMarkdownReport(data);
    
    return {
      'stdout': textSummary,
      [jsonFilename]: JSON.stringify(data, null, 2),
      [mdFilename]: markdownReport,
    };
  } catch (error) {
    console.error(`\n❌ Error generating summary: ${error.message}\n`);
    console.error(`Stack trace: ${error.stack}\n`);
    
    // Fallback: Save minimal results even if formatting fails
    const fallbackMarkdown = `# Test Results - ${env} - ${testName}\n\n` +
      `**Date:** ${new Date().toISOString()}\n\n` +
      `**Environment:** ${env}\n\n` +
      `**Status:** Test completed but summary generation failed\n\n` +
      `**Error:** ${error.message}\n\n` +
      `See JSON file for raw data.\n`;
    
    return {
      'stdout': `Test completed. Error generating formatted summary: ${error.message}`,
      [jsonFilename]: JSON.stringify(data, null, 2),
      [mdFilename]: fallbackMarkdown,
    };
  }
}

function generateTextSummary(data) {
  let output = '\n';
  output += '╔════════════════════════════════════════════════════════════╗\n';
  output += '║            FULL SYSTEM LOAD TEST RESULTS                   ║\n';
  output += '╚════════════════════════════════════════════════════════════╝\n\n';

  output += `🌍 Environment: ${ENVIRONMENT.toUpperCase()}\n`;
  output += `🔗 Base URL: ${BASE_URL}\n`;
  output += `📅 Completed: ${new Date().toISOString()}\n\n`;

  output += '📊 Test Summary:\n';
  output += `  Duration: ${((data.state?.testRunDurationMs || 0) / 1000 / 60).toFixed(2)} minutes\n`;
  output += `  Peak VUs: ${data.metrics.vus?.values.max || 0}\n`;
  output += `  Total Requests: ${data.metrics.http_reqs?.values.count || 0}\n`;
  output += `  Request Rate: ${(data.metrics.http_reqs?.values.rate || 0).toFixed(2)} req/s\n`;
  output += `  Successful Requests: ${data.metrics.successful_requests?.values.count || 0}\n\n`;

  output += '⏱️  Response Times:\n';
  output += `  Average: ${(data.metrics.http_req_duration?.values.avg || 0).toFixed(2)}ms\n`;
  output += `  Median:  ${(data.metrics.http_req_duration?.values.med || 0).toFixed(2)}ms\n`;
  output += `  P90:     ${(data.metrics.http_req_duration?.values['p(90)'] || 0).toFixed(2)}ms\n`;
  output += `  P95:     ${(data.metrics.http_req_duration?.values['p(95)'] || 0).toFixed(2)}ms\n`;
  output += `  P99:     ${(data.metrics.http_req_duration?.values['p(99)'] || 0).toFixed(2)}ms\n\n`;

  const errorRateValue = ((data.metrics.error_rate?.values.rate || 0) * 100).toFixed(2);
  const totalChecks = (data.metrics.checks?.values.passes || 0) + (data.metrics.checks?.values.fails || 0);
  
  output += '✅ Success Metrics:\n';
  output += `  Error Rate: ${errorRateValue}%\n`;
  output += `  Checks Passed: ${data.metrics.checks?.values.passes || 0} / ${totalChecks}\n\n`;

  output += '════════════════════════════════════════════════════════════\n\n';

  return output;
}

function generateMarkdownReport(data) {
  const timestamp = new Date().toISOString();
  const errorRateValue = ((data.metrics.error_rate?.values.rate || 0) * 100).toFixed(2);

  let md = `# Full System Load Test Results - ${ENVIRONMENT.toUpperCase()}\n\n`;
  md += `**Generated**: ${timestamp}\n\n`;
  md += `**Environment**: ${ENVIRONMENT}\n\n`;
  md += `**Base URL**: ${BASE_URL}\n\n`;
  
  md += `## Test Summary\n\n`;
  md += `| Metric | Value |\n`;
  md += `|--------|-------|\n`;
  md += `| Duration | ${(data.state.testRunDurationMs / 1000 / 60).toFixed(2)} minutes |\n`;
  md += `| Peak VUs | ${data.metrics.vus.values.max} |\n`;
  md += `| Total Requests | ${data.metrics.http_reqs.values.count} |\n`;
  md += `| Request Rate | ${data.metrics.http_reqs.values.rate.toFixed(2)} req/s |\n`;
  md += `| Successful Requests | ${data.metrics.successful_requests.values.count} |\n`;
  md += `| Error Rate | ${errorRateValue}% |\n\n`;

  md += `## Response Times\n\n`;
  md += `| Percentile | Time (ms) |\n`;
  md += `|------------|----------|\n`;
  md += `| Average | ${data.metrics.http_req_duration.values.avg.toFixed(2)} |\n`;
  md += `| Median | ${data.metrics.http_req_duration.values.med.toFixed(2)} |\n`;
  md += `| P90 | ${data.metrics.http_req_duration.values['p(90)'].toFixed(2)} |\n`;
  md += `| P95 | ${data.metrics.http_req_duration.values['p(95)'].toFixed(2)} |\n`;
  md += `| P99 | ${data.metrics.http_req_duration.values['p(99)'].toFixed(2)} |\n\n`;

  md += `## Scenario Distribution\n\n`;
  md += `Tests include:\n`;
  md += `- Browse Gyms (anonymous)\n`;
  md += `- Authentication (register + login)\n`;
  md += `- Gym Details\n`;
  md += `- Full User Journey (register → login → browse)\n\n`;

  md += `---\n\n`;
  md += `*Generated by k6 load testing tool*\n`;

  return md;
}
