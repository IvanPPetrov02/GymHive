# GymHive Load Testing Guide

## Prerequisites

Install k6:
- **Windows (via Chocolatey)**: `choco install k6`
- **Windows (via winget)**: `winget install k6 --source winget`
- **Manual**: Download from https://k6.io/docs/get-started/installation/

## Running Load Tests

### 1. Start the Application
```bash
# Make sure Docker is running, then:
docker-compose up -d
```

### 2. Run Individual Tests

**Authentication Service Load Test:**
```bash
k6 run load-tests/auth-load-test.js
```
- Tests user registration and login
- Ramps up to 200 concurrent users
- Duration: ~5 minutes

**Gym Service Load Test:**
```bash
k6 run load-tests/gym-load-test.js
```
- Tests gym and gym group endpoints
- Ramps up to 300 concurrent users
- Duration: ~5 minutes

**Full System Stress Test:**
```bash
k6 run load-tests/full-system-test.js
```
- Tests all services with realistic user scenarios
- Ramps up to 500 concurrent users
- Duration: ~9.5 minutes
- **Recommended for Kubernetes evaluation**

### 3. Run with Prometheus Integration (Optional)

If you have Prometheus running:
```bash
k6 run --out experimental-prometheus-rw load-tests/full-system-test.js
```

## Understanding Results

### Key Metrics to Watch:

1. **http_req_duration**: Response time
   - p(95): 95th percentile - most users experience this or better
   - p(99): 99th percentile
   - Target: <500ms for good UX

2. **http_req_failed**: Failed requests
   - Should be <1% in production

3. **iterations**: Completed user scenarios
   - Higher is better

4. **vus (Virtual Users)**: Concurrent users
   - Follows the stages defined in test

### When Do You Need Kubernetes?

Consider Kubernetes if you see:
- ❌ **High error rates** (>10%) at 200+ users
- ❌ **Slow response times** (p95 >1s)
- ❌ **Memory/CPU exhaustion** in containers
- ❌ **Database connection pool exhaustion**
- ✅ **Need auto-scaling** for traffic spikes
- ✅ **Need zero-downtime deployments**
- ✅ **Need automatic failover**

### Docker Compose is Fine if:
- ✅ Response times stay under 500ms
- ✅ Error rate stays under 5%
- ✅ Resources are stable
- ✅ Traffic is predictable

## Monitoring During Tests

### Check Container Resources:
```bash
docker stats
```

### Check Logs:
```bash
# All services
docker-compose logs -f

# Specific service
docker-compose logs -f gym-service
```

### Access Grafana (if configured):
```
http://localhost:3001
```

## Test Scenarios Explained

### auth-load-test.js
- Simulates user registration and login
- Moderate load (200 users peak)
- Tests authentication service isolation

### gym-load-test.js
- Simulates browsing gyms and groups
- Higher load (300 users peak)
- Tests read-heavy operations

### full-system-test.js
- Realistic user behavior (browsing, auth, memberships)
- Heavy load (500 users peak)
- **Best for evaluating production readiness**
- Tests inter-service communication
- Stress tests database connections

## Next Steps After Testing

1. **Review Results**: Check if thresholds passed
2. **Analyze Bottlenecks**: Identify slow services
3. **Optimize**: 
   - Add database indexes
   - Implement caching
   - Optimize queries
4. **Re-test**: Run tests again after optimization
5. **Decide**: Kubernetes vs Docker Compose based on results
