import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate, Trend, Counter } from 'k6/metrics';
import { textSummary } from 'https://jslib.k6.io/k6-summary/0.0.1/index.js';

// Custom metrics
const errorRate = new Rate('error_rate');
const registerDuration = new Trend('register_duration');
const loginDuration = new Trend('login_duration');
const registerSuccess = new Counter('register_success');
const loginSuccess = new Counter('login_success');

// Configuration - can be overridden via environment variables
const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';
const ENVIRONMENT = __ENV.ENVIRONMENT || 'docker';

// Test configuration optimized for different environments
const configs = {
  docker: {
    stages: [
      { duration: '30s', target: 25 },    // Warm up
      { duration: '1m', target: 50 },     // Ramp to 50
      { duration: '2m', target: 100 },    // Sustained at 100
      { duration: '1m', target: 150 },    // Peak at 150
      { duration: '30s', target: 0 },     // Ramp down
    ],
    thresholds: {
      http_req_duration: ['p(95)<2000'],
      error_rate: ['rate<0.05'],
    },
  },
  kubernetes: {
    stages: [
      { duration: '1m', target: 50 },     // Warm up
      { duration: '2m', target: 150 },    // Ramp to 150
      { duration: '3m', target: 300 },    // Sustained at 300
      { duration: '2m', target: 500 },    // Peak at 500
      { duration: '1m', target: 0 },      // Ramp down
    ],
    thresholds: {
      http_req_duration: ['p(95)<1000'],
      error_rate: ['rate<0.01'],
    },
  },
  hpa: {
    stages: [
      { duration: '1m', target: 100 },    // Initial load
      { duration: '2m', target: 300 },    // Trigger scaling
      { duration: '3m', target: 500 },    // Sustain high load
      { duration: '2m', target: 1000 },   // Peak load
      { duration: '5m', target: 1000 },   // Sustained peak (observe scaling)
      { duration: '2m', target: 100 },    // Scale down
      { duration: '1m', target: 0 },      // Cool down
    ],
    thresholds: {
      http_req_duration: ['p(95)<800'],
      error_rate: ['rate<0.01'],
    },
  },
};

export const options = configs[ENVIRONMENT];

export default function () {
  const timestamp = Date.now();
  const vuId = __VU;
  const iteration = __ITER;
  const uniqueEmail = `loadtest_${vuId}_${timestamp}_${iteration}@gymhive.test`;

  // Test 1: Register a new user
  const registerPayload = JSON.stringify({
    email: uniqueEmail,
    password: 'LoadTest123!',
    name: `User${vuId}`,
    surname: `Test${iteration}`,
    role: 'User'
  });

  const params = {
    headers: { 'Content-Type': 'application/json' },
    tags: { endpoint: 'register', environment: ENVIRONMENT },
  };

  const registerStartTime = Date.now();
  const registerRes = http.post(
    `${BASE_URL}/api/auth/register`,
    registerPayload,
    params
  );
  const registerEndTime = Date.now();

  const registerCheck = check(registerRes, {
    'register: status 200': (r) => r.status === 200,
    'register: has message': (r) => {
      try {
        return r.json('message') !== undefined;
      } catch (e) {
        return false;
      }
    },
  });

  if (registerCheck) {
    registerSuccess.add(1);
    registerDuration.add(registerEndTime - registerStartTime);
  } else {
    errorRate.add(1);
  }

  sleep(0.3);

  // Test 2: Login with the newly created user
  const loginPayload = JSON.stringify({
    email: uniqueEmail,
    password: 'LoadTest123!',
  });

  const loginParams = {
    headers: { 'Content-Type': 'application/json' },
    tags: { endpoint: 'login', environment: ENVIRONMENT },
  };

  const loginStartTime = Date.now();
  const loginRes = http.post(
    `${BASE_URL}/api/auth/login`,
    loginPayload,
    loginParams
  );
  const loginEndTime = Date.now();

  const loginCheck = check(loginRes, {
    'login: status 200': (r) => r.status === 200,
    'login: returns token': (r) => {
      try {
        const json = r.json();
        return json.token !== undefined && json.token.length > 0;
      } catch (e) {
        return false;
      }
    },
  });

  if (loginCheck) {
    loginSuccess.add(1);
    loginDuration.add(loginEndTime - loginStartTime);
  } else {
    errorRate.add(1);
  }

  sleep(0.5);
}

export function handleSummary(data) {
  const timestamp = new Date().toISOString().replace(/[:.]/g, '-').slice(0, -5);
  const testName = 'auth-comprehensive-test';
  const env = ENVIRONMENT;
  const filename = `../results/${env}-${testName}-${timestamp}.json`;
  const markdownFilename = `../results/${env}-${testName}-${timestamp}.md`;

  console.log(`\n📊 Saving results to: ${markdownFilename}\n`);

  try {
    const textSummary = generateTextSummary(data);
    const markdownReport = generateMarkdownReport(data);
    
    return {
      'stdout': textSummary,
      [filename]: JSON.stringify(data, null, 2),
      [markdownFilename]: markdownReport,
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
      [filename]: JSON.stringify(data, null, 2),
      [markdownFilename]: fallbackMarkdown,
    };
  }
}

function generateTextSummary(data) {
  let output = '\n';
  output += '╔════════════════════════════════════════════════════════════╗\n';
  output += '║          AUTHENTICATION SERVICE LOAD TEST RESULTS          ║\n';
  output += '╚════════════════════════════════════════════════════════════╝\n\n';

  output += `🌍 Environment: ${ENVIRONMENT.toUpperCase()}\n`;
  output += `🔗 Base URL: ${BASE_URL}\n`;
  output += `📅 Completed: ${new Date().toISOString()}\n\n`;

  output += '📊 Test Summary:\n';
  output += `  Duration: ${(data.state.testRunDurationMs / 1000 / 60).toFixed(2)} minutes\n`;
  output += `  Peak VUs: ${data.metrics.vus.values.max}\n`;
  output += `  Total Requests: ${data.metrics.http_reqs.values.count}\n`;
  output += `  Request Rate: ${data.metrics.http_reqs.values.rate.toFixed(2)} req/s\n`;
  output += `  Iterations: ${data.metrics.iterations.values.count}\n\n`;

  output += '⏱️  Response Times:\n';
  output += `  Average: ${(data.metrics.http_req_duration?.values.avg || 0).toFixed(2)}ms\n`;
  output += `  Median:  ${(data.metrics.http_req_duration?.values.med || 0).toFixed(2)}ms\n`;
  output += `  P90:     ${(data.metrics.http_req_duration?.values['p(90)'] || 0).toFixed(2)}ms\n`;
  output += `  P95:     ${(data.metrics.http_req_duration?.values['p(95)'] || 0).toFixed(2)}ms\n`;
  output += `  P99:     ${(data.metrics.http_req_duration?.values['p(99)'] || 0).toFixed(2)}ms\n`;
  output += `  Max:     ${(data.metrics.http_req_duration?.values.max || 0).toFixed(2)}ms\n\n`;

  const successRate = ((1 - (data.metrics.http_req_failed?.values.rate || 0)) * 100).toFixed(2);
  const errorRateValue = ((data.metrics.error_rate?.values.rate || 0) * 100).toFixed(2);
  const totalChecks = (data.metrics.checks?.values.passes || 0) + (data.metrics.checks?.values.fails || 0);
  
  output += '✅ Success Metrics:\n';
  output += `  HTTP Success Rate: ${successRate}%\n`;
  output += `  Error Rate: ${errorRateValue}%\n`;
  output += `  Checks Passed: ${data.metrics.checks?.values.passes || 0} / ${totalChecks}\n\n`;

  if (data.metrics.register_duration) {
    output += '📝 Registration Performance:\n';
    output += `  Count: ${data.metrics.register_success.values.count}\n`;
    output += `  Avg: ${(data.metrics.register_duration.values.avg || 0).toFixed(2)}ms\n`;
    output += `  P95: ${(data.metrics.register_duration.values['p(95)'] || 0).toFixed(2)}ms\n`;
    if (data.metrics.register_duration.values['p(99)']) {
      output += `  P99: ${data.metrics.register_duration.values['p(99)'].toFixed(2)}ms\n`;
    }
    output += '\n';
  }

  if (data.metrics.login_duration) {
    output += '🔐 Login Performance:\n';
    output += `  Count: ${data.metrics.login_success.values.count}\n`;
    output += `  Avg: ${(data.metrics.login_duration.values.avg || 0).toFixed(2)}ms\n`;
    output += `  P95: ${(data.metrics.login_duration.values['p(95)'] || 0).toFixed(2)}ms\n`;
    if (data.metrics.login_duration.values['p(99)']) {
      output += `  P99: ${data.metrics.login_duration.values['p(99)'].toFixed(2)}ms\n`;
    }
    output += '\n';
  }

  // Threshold results
  output += '🎯 Threshold Results:\n';
  const thresholds = data.metrics;
  for (const [metric, values] of Object.entries(thresholds)) {
    if (values.thresholds) {
      for (const [threshold, result] of Object.entries(values.thresholds)) {
        const status = result.ok ? '✅' : '❌';
        output += `  ${status} ${metric}: ${threshold}\n`;
      }
    }
  }

  output += '\n════════════════════════════════════════════════════════════\n\n';

  return output;
}

function generateMarkdownReport(data) {
  const timestamp = new Date().toISOString();
  const successRate = ((1 - (data.metrics.http_req_failed?.values.rate || 0)) * 100).toFixed(2);
  const errorRateValue = ((data.metrics.error_rate?.values.rate || 0) * 100).toFixed(2);

  let md = `# Authentication Load Test Results - ${ENVIRONMENT.toUpperCase()}\n\n`;
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
  md += `| Iterations | ${data.metrics.iterations.values.count} |\n`;
  md += `| HTTP Success Rate | ${successRate}% |\n`;
  md += `| Error Rate | ${errorRateValue}% |\n\n`;

  md += `## Response Times\n\n`;
  md += `| Percentile | Time (ms) |\n`;
  md += `|------------|----------|\n`;
  md += `| Average | ${data.metrics.http_req_duration.values.avg.toFixed(2)} |\n`;
  md += `| Median | ${data.metrics.http_req_duration.values.med.toFixed(2)} |\n`;
  md += `| P90 | ${data.metrics.http_req_duration.values['p(90)'].toFixed(2)} |\n`;
  md += `| P95 | ${data.metrics.http_req_duration.values['p(95)'].toFixed(2)} |\n`;
  md += `| P99 | ${data.metrics.http_req_duration.values['p(99)'].toFixed(2)} |\n`;
  md += `| Max | ${data.metrics.http_req_duration.values.max.toFixed(2)} |\n\n`;

  md += `## Endpoint Performance\n\n`;
  
  if (data.metrics.register_duration) {
    md += `### Registration\n\n`;
    md += `| Metric | Value |\n`;
    md += `|--------|-------|\n`;
    md += `| Successful Registrations | ${data.metrics.register_success.values.count} |\n`;
    md += `| Average Duration | ${data.metrics.register_duration.values.avg.toFixed(2)}ms |\n`;
    md += `| P95 Duration | ${data.metrics.register_duration.values['p(95)'].toFixed(2)}ms |\n`;
    md += `| P99 Duration | ${data.metrics.register_duration.values['p(99)'].toFixed(2)}ms |\n\n`;
  }

  if (data.metrics.login_duration) {
    md += `### Login\n\n`;
    md += `| Metric | Value |\n`;
    md += `|--------|-------|\n`;
    md += `| Successful Logins | ${data.metrics.login_success.values.count} |\n`;
    md += `| Average Duration | ${data.metrics.login_duration.values.avg.toFixed(2)}ms |\n`;
    md += `| P95 Duration | ${data.metrics.login_duration.values['p(95)'].toFixed(2)}ms |\n`;
    md += `| P99 Duration | ${data.metrics.login_duration.values['p(99)'].toFixed(2)}ms |\n\n`;
  }

  md += `## Threshold Results\n\n`;
  md += `| Metric | Threshold | Result |\n`;
  md += `|--------|-----------|--------|\n`;
  
  const thresholds = data.metrics;
  for (const [metric, values] of Object.entries(thresholds)) {
    if (values.thresholds) {
      for (const [threshold, result] of Object.entries(values.thresholds)) {
        const status = result.ok ? '✅ Pass' : '❌ Fail';
        md += `| ${metric} | ${threshold} | ${status} |\n`;
      }
    }
  }

  md += `\n## Analysis\n\n`;
  
  if (parseFloat(successRate) >= 99.5) {
    md += `✅ **Excellent** - Success rate above 99.5%\n\n`;
  } else if (parseFloat(successRate) >= 95) {
    md += `⚠️ **Good** - Success rate above 95% but some failures occurred\n\n`;
  } else {
    md += `❌ **Poor** - Success rate below 95%, investigation needed\n\n`;
  }

  const p95 = data.metrics.http_req_duration.values['p(95)'];
  if (p95 < 500) {
    md += `✅ **Fast** - P95 response time under 500ms\n\n`;
  } else if (p95 < 1000) {
    md += `⚠️ **Acceptable** - P95 response time under 1000ms\n\n`;
  } else if (p95 < 2000) {
    md += `⚠️ **Slow** - P95 response time above 1000ms\n\n`;
  } else {
    md += `❌ **Very Slow** - P95 response time above 2000ms\n\n`;
  }

  md += `---\n\n`;
  md += `*Generated by k6 load testing tool*\n`;

  return md;
}
