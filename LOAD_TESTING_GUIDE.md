# Load Testing & Monitoring Setup Complete! 🚀

## What's Been Set Up

### 1. Load Testing with k6
- ✅ **auth-load-test.js** - Tests authentication (200 users peak)
- ✅ **gym-load-test.js** - Tests gym services (300 users peak)
- ✅ **full-system-test.js** - Complete stress test (500 users peak)

### 2. Monitoring Stack
- ✅ **Prometheus** - Collects metrics from all services (Port 9090)
- ✅ **Grafana** - Visualizes metrics in dashboards (Port 3001)

## Quick Start Guide

### Step 1: Install k6

**Option A - Using Chocolatey (Recommended):**
```powershell
choco install k6
```

**Option B - Using winget:**
```powershell
winget install k6 --source winget
```

**Option C - Manual Download:**
Visit: https://k6.io/docs/get-started/installation/

### Step 2: Start All Services with Monitoring
```powershell
cd "c:\Users\vanit\Desktop\CSS - Semester 6\individual project\GymHive"
docker-compose up -d
```

Wait ~30 seconds for all services to start.

### Step 3: Verify Services Are Running
```powershell
docker-compose ps
```

All services should show "Up" status.

### Step 4: Access Monitoring Dashboards

**Grafana Dashboard:**
- URL: http://localhost:3001
- Username: `admin`
- Password: `admin`
- First time: Add Prometheus data source (http://prometheus:9090)

**Prometheus:**
- URL: http://localhost:9090

### Step 5: Run Load Tests

**Start with Light Load (Auth Test):**
```powershell
k6 run load-tests/auth-load-test.js
```

**Medium Load (Gym Test):**
```powershell
k6 run load-tests/gym-load-test.js
```

**Full Stress Test (Recommended for Kubernetes Decision):**
```powershell
k6 run load-tests/full-system-test.js
```

### Step 6: Monitor During Tests

**Watch real-time container stats:**
```powershell
docker stats
```

**Watch service logs:**
```powershell
# All services
docker-compose logs -f

# Specific service
docker-compose logs -f gym-service
docker-compose logs -f membership-service
```

**Watch in Grafana:**
- Go to http://localhost:3001
- Create a new dashboard
- Add panels with Prometheus queries

## What to Look For

### ✅ Good Signs (Docker Compose is Fine)
- Response times p(95) < 500ms
- Error rate < 5%
- CPU usage < 70% per container
- Memory usage stable
- All health checks passing

### ❌ Warning Signs (Consider Kubernetes)
- Response times p(95) > 1s
- Error rate > 10%
- CPU usage sustained at 90%+
- Memory constantly growing
- Frequent container restarts
- Database connection errors

## Understanding k6 Output

```
     execution: local
        script: load-tests/full-system-test.js
        output: -

     scenarios: (100.00%) 1 scenario, 500 max VUs, 10m0s max duration
              * default: Up to 500 looping VUs for 9m30s over 6 stages

     ✓ http_req_duration..............: avg=245ms  p(95)=450ms  p(99)=890ms  
     ✗ errors.........................: 3.2%      
       http_reqs......................: 45000
       iterations.....................: 15000
       vus............................: 500
```

**Key Metrics:**
- **http_req_duration p(95)**: 95% of requests completed in this time
- **errors**: Percentage of failed requests
- **http_reqs**: Total number of HTTP requests made
- **iterations**: Number of complete test scenarios
- **vus**: Current number of virtual users

## Next Steps Based on Results

### Scenario A: Tests Pass ✅
- Docker Compose is sufficient
- Consider implementing:
  - Redis caching
  - Database indexing
  - Connection pooling optimization

### Scenario B: Tests Show Strain ⚠️
- Consider Kubernetes for:
  - Horizontal pod autoscaling
  - Better resource management
  - Rolling updates
  - Self-healing capabilities

### Scenario C: Tests Fail ❌
- First optimize:
  - Add database indexes
  - Implement caching (Redis)
  - Optimize slow queries
  - Increase connection pools
- Then re-test
- If still failing → Kubernetes needed

## Grafana Dashboard Setup (Optional)

1. Open Grafana: http://localhost:3001
2. Login: admin/admin
3. Add Data Source:
   - Type: Prometheus
   - URL: http://prometheus:9090
   - Save & Test
4. Import Dashboard:
   - Dashboard ID: 1860 (Node Exporter Full)
   - Or create custom dashboard

## Useful Prometheus Queries

**Request Rate:**
```
rate(http_requests_total[5m])
```

**Response Time:**
```
histogram_quantile(0.95, rate(http_request_duration_seconds_bucket[5m]))
```

**Error Rate:**
```
rate(http_requests_total{status=~"5.."}[5m])
```

## Clean Up After Testing

**Stop all services:**
```powershell
docker-compose down
```

**Remove volumes (fresh start):**
```powershell
docker-compose down -v
```

## Expected Test Duration

- **auth-load-test.js**: ~5 minutes
- **gym-load-test.js**: ~5 minutes  
- **full-system-test.js**: ~9.5 minutes

## Troubleshooting

**k6 not found:**
- Make sure k6 is installed
- Restart PowerShell after installation

**Services not responding:**
- Check Docker: `docker-compose ps`
- Check logs: `docker-compose logs`
- Verify health: `docker-compose ps`

**High error rates immediately:**
- Services might not be ready
- Wait 30s after `docker-compose up`
- Check service health

**Database connection errors:**
- Increase connection pool size in appsettings
- Add more database resources
- Consider connection pooling

---

## Ready to Test?

1. Install k6
2. Run: `docker-compose up -d`
3. Wait 30 seconds
4. Run: `k6 run load-tests/full-system-test.js`
5. Watch the results!

The test will tell you if you need Kubernetes or if Docker Compose is sufficient! 🎯
