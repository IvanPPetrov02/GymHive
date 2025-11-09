import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate, Trend, Counter } from 'k6/metrics';

// Custom metrics
let errorRate = new Rate('errors');
let loginDuration = new Trend('login_duration');
let gymListDuration = new Trend('gym_list_duration');
let membershipDuration = new Trend('membership_duration');
let requestCounter = new Counter('total_requests');

// Country-Scale Peak Load Test (Compressed - 15 minutes)
// Simulating country-wide peak hours
// Target: 500 gyms, 5,000-10,000 concurrent users during peak

export let options = {
    stages: [
        { duration: '1m', target: 1000 },    // Warm up
        { duration: '2m', target: 3000 },    // Ramp to 3k
        { duration: '2m', target: 5000 },    // Reach 5k (realistic peak)
        { duration: '3m', target: 5000 },    // Sustained 5k
        { duration: '2m', target: 8000 },    // Stress test to 8k
        { duration: '2m', target: 10000 },   // MAXIMUM STRESS (10k)
        { duration: '2m', target: 10000 },   // Hold at maximum
        { duration: '1m', target: 0 },       // Cool down
    ],
    
    thresholds: {
        'http_req_duration': ['p(95)<2000', 'p(99)<5000'],  // 95% under 2s, 99% under 5s
        'http_req_failed': ['rate<0.10'],                    // Less than 10% errors (realistic)
        'errors': ['rate<0.10'],                             
        'http_reqs': ['rate>200'],                           // At least 200 req/s
    },
};

const BASE_URL = 'http://localhost:8080';

// Realistic user data
function generateUser() {
    const timestamp = Date.now();
    const random = Math.floor(Math.random() * 1000000);
    return {
        username: `user_${timestamp}_${random}`,
        email: `user${timestamp}${random}@gym.com`,
        password: 'TestPass123!',
        firstName: `User${random}`,
        lastName: `Test${timestamp}`
    };
}

export default function() {
    requestCounter.add(1);
    
    // Realistic user behavior
    const action = Math.random();
    
    if (action < 0.10) {
        // 10% - New registrations
        registerNewUser();
    } else if (action < 0.50) {
        // 40% - Browse gyms (public, no auth)
        browseGyms();
    } else if (action < 0.75) {
        // 25% - Login attempt + browse
        loginAndBrowse();
    } else {
        // 25% - Check memberships
        checkMemberships();
    }
    
    // Think time (0.5-3 seconds)
    sleep(Math.random() * 2.5 + 0.5);
}

function registerNewUser() {
    const user = generateUser();
    
    let res = http.post(`${BASE_URL}/auth/register`, JSON.stringify({
        username: user.username,
        email: user.email,
        password: user.password,
        firstName: user.firstName,
        lastName: user.lastName
    }), {
        headers: { 'Content-Type': 'application/json' },
        tags: { name: 'Register' },
    });
    
    let success = check(res, {
        'registration success': (r) => r.status === 200 || r.status === 201,
    });
    
    if (!success) errorRate.add(1);
}

function browseGyms() {
    let res = http.get(`${BASE_URL}/gym/gyms`, {
        tags: { name: 'BrowseGyms' },
    });
    
    gymListDuration.add(res.timings.duration);
    
    let success = check(res, {
        'gyms loaded': (r) => r.status === 200,
    });
    
    if (!success) errorRate.add(1);
}

function loginAndBrowse() {
    // Attempt login
    let loginRes = http.post(`${BASE_URL}/auth/login`, JSON.stringify({
        username: `user_${Math.floor(Math.random() * 1000000)}`,
        password: 'TestPass123!'
    }), {
        headers: { 'Content-Type': 'application/json' },
        tags: { name: 'Login' },
    });
    
    loginDuration.add(loginRes.timings.duration);
    
    sleep(0.5);
    
    // Browse gyms
    let gymsRes = http.get(`${BASE_URL}/gym/gyms`, {
        tags: { name: 'ListGyms' },
    });
    
    gymListDuration.add(gymsRes.timings.duration);
    
    check(gymsRes, {
        'gyms browse success': (r) => r.status === 200,
    });
}

function checkMemberships() {
    let res = http.get(`${BASE_URL}/membership/memberships`, {
        tags: { name: 'Memberships' },
    });
    
    membershipDuration.add(res.timings.duration);
    
    // Accept 401 as valid (expected for unauth users)
    check(res, {
        'membership endpoint responds': (r) => r.status === 200 || r.status === 401,
    });
}

export function handleSummary(data) {
    console.log('\n========================================');
    console.log('  COUNTRY-SCALE LOAD TEST RESULTS');
    console.log('========================================\n');
    
    console.log('📊 Test Summary:');
    console.log(`  Duration: ${(data.state.testRunDurationMs / 1000 / 60).toFixed(2)} minutes`);
    console.log(`  Peak VUs: ${(data.metrics.vus_max && data.metrics.vus_max.values && data.metrics.vus_max.values.max) || 0}`);
    console.log(`  Total Requests: ${(data.metrics.http_reqs && data.metrics.http_reqs.values && data.metrics.http_reqs.values.count) || 0}`);
    console.log(`  Request Rate: ${((data.metrics.http_reqs && data.metrics.http_reqs.values && data.metrics.http_reqs.values.rate) || 0).toFixed(2)} req/s\n`);
    
    console.log('⏱️  Response Times:');
    console.log(`  Average: ${((data.metrics.http_req_duration && data.metrics.http_req_duration.values && data.metrics.http_req_duration.values.avg) || 0).toFixed(2)}ms`);
    console.log(`  Median:  ${((data.metrics.http_req_duration && data.metrics.http_req_duration.values && data.metrics.http_req_duration.values.med) || 0).toFixed(2)}ms`);
    console.log(`  P95:     ${((data.metrics.http_req_duration && data.metrics.http_req_duration.values && data.metrics.http_req_duration.values['p(95)']) || 0).toFixed(2)}ms`);
    console.log(`  P99:     ${((data.metrics.http_req_duration && data.metrics.http_req_duration.values && data.metrics.http_req_duration.values['p(99)']) || 0).toFixed(2)}ms`);
    console.log(`  Max:     ${((data.metrics.http_req_duration && data.metrics.http_req_duration.values && data.metrics.http_req_duration.values.max) || 0).toFixed(2)}ms\n`);
    
    console.log('✅ Success Metrics:');
    console.log(`  Failed Requests: ${(((data.metrics.http_req_failed && data.metrics.http_req_failed.values && data.metrics.http_req_failed.values.rate) || 0) * 100).toFixed(2)}%`);
    console.log(`  Error Rate: ${(((data.metrics.errors && data.metrics.errors.values && data.metrics.errors.values.rate) || 0) * 100).toFixed(2)}%\n`);
    
    console.log('🎯 Endpoint Performance:');
    console.log(`  Login Avg: ${((data.metrics.login_duration && data.metrics.login_duration.values && data.metrics.login_duration.values.avg) || 0).toFixed(2)}ms`);
    console.log(`  Gym List Avg: ${((data.metrics.gym_list_duration && data.metrics.gym_list_duration.values && data.metrics.gym_list_duration.values.avg) || 0).toFixed(2)}ms`);
    console.log(`  Membership Avg: ${((data.metrics.membership_duration && data.metrics.membership_duration.values && data.metrics.membership_duration.values.avg) || 0).toFixed(2)}ms\n`);
    
    return {
        'stdout': textSummary(data),
        'country-scale-results.json': JSON.stringify(data, null, 2),
    };
}

function textSummary(data) {
    return ''; // Console.log above handles the output
}
