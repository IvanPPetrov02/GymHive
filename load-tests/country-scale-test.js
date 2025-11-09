import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate, Trend, Counter } from 'k6/metrics';

// Custom metrics
let errorRate = new Rate('errors');
let loginDuration = new Trend('login_duration');
let gymListDuration = new Trend('gym_list_duration');
let membershipDuration = new Trend('membership_duration');
let requestCounter = new Counter('total_requests');

// Country-Scale Load Test
// Simulating usage from gyms across an entire country
// Assumptions:
// - 500 gyms across the country
// - Each gym has 200-500 active members
// - Peak hours: 6-9am, 5-9pm (50% of daily users)
// - Average 100,000 daily active users
// - Peak concurrent users: 5,000-10,000

export let options = {
    scenarios: {
        // Scenario 1: Morning Rush (6-9am)
        morning_rush: {
            executor: 'ramping-vus',
            startTime: '0s',
            stages: [
                { duration: '2m', target: 2000 },   // Ramp up to 2000 users
                { duration: '5m', target: 5000 },   // Peak morning traffic
                { duration: '3m', target: 5000 },   // Sustained peak
                { duration: '2m', target: 1000 },   // Cool down
            ],
            gracefulRampDown: '30s',
        },
        
        // Scenario 2: Mid-day Steady (9am-5pm)
        midday_steady: {
            executor: 'ramping-vus',
            startTime: '12m',
            stages: [
                { duration: '10m', target: 1500 },  // Steady mid-day traffic
                { duration: '5m', target: 2000 },   // Lunch rush
                { duration: '5m', target: 1500 },   // Back to steady
            ],
            gracefulRampDown: '30s',
        },
        
        // Scenario 3: Evening Rush (5-9pm) - PEAK LOAD
        evening_rush: {
            executor: 'ramping-vus',
            startTime: '32m',
            stages: [
                { duration: '3m', target: 3000 },   // Ramp up
                { duration: '5m', target: 7000 },   // Peak evening traffic
                { duration: '5m', target: 10000 },  // MAXIMUM PEAK
                { duration: '5m', target: 10000 },  // Sustained maximum
                { duration: '3m', target: 5000 },   // Wind down
                { duration: '2m', target: 1000 },   // Cool down
            ],
            gracefulRampDown: '30s',
        },
        
        // Scenario 4: Night Time (9pm-6am) - Low traffic
        night_time: {
            executor: 'ramping-vus',
            startTime: '55m',
            stages: [
                { duration: '5m', target: 500 },    // Night owls
            ],
            gracefulRampDown: '30s',
        },
    },
    
    thresholds: {
        'http_req_duration': ['p(95)<2000', 'p(99)<5000'],  // 95% under 2s, 99% under 5s
        'http_req_failed': ['rate<0.05'],                    // Less than 5% errors
        'errors': ['rate<0.05'],                             // Less than 5% check failures
        'http_reqs': ['rate>500'],                           // At least 500 req/s during peak
    },
};

const BASE_URL = 'http://localhost:8080';

// Realistic user data for registration
function generateUser() {
    const timestamp = Date.now();
    const random = Math.floor(Math.random() * 100000);
    return {
        username: `user_${timestamp}_${random}`,
        email: `user${timestamp}${random}@gym.com`,
        password: 'TestPass123!',
        firstName: `User${random}`,
        lastName: `Test${timestamp}`
    };
}

// Simulate realistic user behavior patterns
export default function() {
    requestCounter.add(1);
    
    // Determine user action based on probability
    const action = Math.random();
    
    if (action < 0.15) {
        // 15% - New user registration
        registerNewUser();
    } else if (action < 0.40) {
        // 25% - User login and browse gyms
        loginAndBrowseGyms();
    } else if (action < 0.70) {
        // 30% - Existing user checks memberships
        checkMemberships();
    } else if (action < 0.85) {
        // 15% - Browse gyms (no auth)
        browseGymsOnly();
    } else {
        // 15% - Full user journey (register -> login -> browse -> membership)
        fullUserJourney();
    }
    
    // Realistic think time between actions (1-5 seconds)
    sleep(Math.random() * 4 + 1);
}

function registerNewUser() {
    const user = generateUser();
    
    let registerRes = http.post(`${BASE_URL}/auth/register`, JSON.stringify({
        username: user.username,
        email: user.email,
        password: user.password,
        firstName: user.firstName,
        lastName: user.lastName
    }), {
        headers: { 'Content-Type': 'application/json' },
        tags: { name: 'Register' },
    });
    
    let success = check(registerRes, {
        'registration status is 200 or 201': (r) => r.status === 200 || r.status === 201,
    });
    
    if (!success) {
        errorRate.add(1);
    }
}

function loginAndBrowseGyms() {
    // Try to login (will fail for non-existent users, which is realistic)
    let loginRes = http.post(`${BASE_URL}/auth/login`, JSON.stringify({
        username: `user_${Math.floor(Math.random() * 1000000)}`,
        password: 'TestPass123!'
    }), {
        headers: { 'Content-Type': 'application/json' },
        tags: { name: 'Login' },
    });
    
    loginDuration.add(loginRes.timings.duration);
    
    // Browse gyms regardless of login success (public endpoint)
    let gymsRes = http.get(`${BASE_URL}/gym/gyms`, {
        tags: { name: 'ListGyms' },
    });
    
    gymListDuration.add(gymsRes.timings.duration);
    
    check(gymsRes, {
        'gyms list loaded': (r) => r.status === 200,
    });
}

function checkMemberships() {
    // Attempt to check memberships (will get 401 for unauthenticated, which is expected)
    let membershipRes = http.get(`${BASE_URL}/membership/memberships`, {
        tags: { name: 'ListMemberships' },
    });
    
    membershipDuration.add(membershipRes.timings.duration);
    
    // Accept both 200 (if somehow authenticated) and 401 (expected)
    check(membershipRes, {
        'membership endpoint responds': (r) => r.status === 200 || r.status === 401,
    });
}

function browseGymsOnly() {
    let gymsRes = http.get(`${BASE_URL}/gym/gyms`, {
        tags: { name: 'BrowseGyms' },
    });
    
    gymListDuration.add(gymsRes.timings.duration);
    
    let success = check(gymsRes, {
        'browse gyms success': (r) => r.status === 200,
    });
    
    if (!success) {
        errorRate.add(1);
    }
}

function fullUserJourney() {
    const user = generateUser();
    
    // Step 1: Register
    let registerRes = http.post(`${BASE_URL}/auth/register`, JSON.stringify({
        username: user.username,
        email: user.email,
        password: user.password,
        firstName: user.firstName,
        lastName: user.lastName
    }), {
        headers: { 'Content-Type': 'application/json' },
        tags: { name: 'FullJourney_Register' },
    });
    
    if (registerRes.status !== 200 && registerRes.status !== 201) {
        errorRate.add(1);
        return;
    }
    
    sleep(1); // Think time
    
    // Step 2: Login
    let loginRes = http.post(`${BASE_URL}/auth/login`, JSON.stringify({
        username: user.username,
        password: user.password
    }), {
        headers: { 'Content-Type': 'application/json' },
        tags: { name: 'FullJourney_Login' },
    });
    
    loginDuration.add(loginRes.timings.duration);
    
    let token = null;
    if (loginRes.status === 200) {
        try {
            const body = JSON.parse(loginRes.body);
            token = body.token;
        } catch (e) {
            errorRate.add(1);
            return;
        }
    }
    
    sleep(1); // Think time
    
    // Step 3: Browse gyms
    let gymsRes = http.get(`${BASE_URL}/gym/gyms`, {
        headers: token ? { 'Authorization': `Bearer ${token}` } : {},
        tags: { name: 'FullJourney_Gyms' },
    });
    
    gymListDuration.add(gymsRes.timings.duration);
    
    check(gymsRes, {
        'full journey gyms loaded': (r) => r.status === 200,
    });
    
    sleep(2); // Think time - user browses
    
    // Step 4: Check memberships (if authenticated)
    if (token) {
        let membershipRes = http.get(`${BASE_URL}/membership/memberships`, {
            headers: { 'Authorization': `Bearer ${token}` },
            tags: { name: 'FullJourney_Memberships' },
        });
        
        membershipDuration.add(membershipRes.timings.duration);
        
        check(membershipRes, {
            'full journey memberships loaded': (r) => r.status === 200 || r.status === 401,
        });
    }
}

export function handleSummary(data) {
    return {
        'stdout': textSummary(data, { indent: ' ', enableColors: true }),
        'country-scale-test-results.json': JSON.stringify(data, null, 2),
    };
}

function textSummary(data, options) {
    const indent = options.indent || '';
    const enableColors = options.enableColors || false;
    
    let summary = '\n';
    summary += `${indent}========================================\n`;
    summary += `${indent}  COUNTRY-SCALE LOAD TEST RESULTS\n`;
    summary += `${indent}========================================\n\n`;
    
    summary += `${indent}📊 Test Summary:\n`;
    summary += `${indent}  Duration: ${(data.state.testRunDurationMs / 1000 / 60).toFixed(2)} minutes\n`;
    summary += `${indent}  Peak VUs: ${(data.metrics.vus_max && data.metrics.vus_max.values && data.metrics.vus_max.values.max) || 0}\n`;
    summary += `${indent}  Total Requests: ${(data.metrics.http_reqs && data.metrics.http_reqs.values && data.metrics.http_reqs.values.count) || 0}\n`;
    summary += `${indent}  Request Rate: ${((data.metrics.http_reqs && data.metrics.http_reqs.values && data.metrics.http_reqs.values.rate) || 0).toFixed(2)} req/s\n\n`;
    
    summary += `${indent}⏱️  Response Times:\n`;
    summary += `${indent}  Average: ${((data.metrics.http_req_duration && data.metrics.http_req_duration.values && data.metrics.http_req_duration.values.avg) || 0).toFixed(2)}ms\n`;
    summary += `${indent}  Median:  ${((data.metrics.http_req_duration && data.metrics.http_req_duration.values && data.metrics.http_req_duration.values.med) || 0).toFixed(2)}ms\n`;
    summary += `${indent}  P95:     ${((data.metrics.http_req_duration && data.metrics.http_req_duration.values && data.metrics.http_req_duration.values['p(95)']) || 0).toFixed(2)}ms\n`;
    summary += `${indent}  P99:     ${((data.metrics.http_req_duration && data.metrics.http_req_duration.values && data.metrics.http_req_duration.values['p(99)']) || 0).toFixed(2)}ms\n`;
    summary += `${indent}  Max:     ${((data.metrics.http_req_duration && data.metrics.http_req_duration.values && data.metrics.http_req_duration.values.max) || 0).toFixed(2)}ms\n\n`;
    
    summary += `${indent}✅ Success Metrics:\n`;
    summary += `${indent}  Failed Requests: ${(((data.metrics.http_req_failed && data.metrics.http_req_failed.values && data.metrics.http_req_failed.values.rate) || 0) * 100).toFixed(2)}%\n`;
    summary += `${indent}  Error Rate: ${(((data.metrics.errors && data.metrics.errors.values && data.metrics.errors.values.rate) || 0) * 100).toFixed(2)}%\n\n`;
    
    summary += `${indent}🎯 Endpoint Performance:\n`;
    summary += `${indent}  Login Avg: ${((data.metrics.login_duration && data.metrics.login_duration.values && data.metrics.login_duration.values.avg) || 0).toFixed(2)}ms\n`;
    summary += `${indent}  Gym List Avg: ${((data.metrics.gym_list_duration && data.metrics.gym_list_duration.values && data.metrics.gym_list_duration.values.avg) || 0).toFixed(2)}ms\n`;
    summary += `${indent}  Membership Avg: ${((data.metrics.membership_duration && data.metrics.membership_duration.values && data.metrics.membership_duration.values.avg) || 0).toFixed(2)}ms\n\n`;
    
    return summary;
}
