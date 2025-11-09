# 🎯 Load Testing & Monitoring - Ready to Go!

## ✅ What's Running

All services have been successfully deployed with monitoring:

### Application Services
- **API Gateway**: http://localhost:5000 ✅
- **Authentication Service**: http://localhost:8080 ✅
- **Gym Service**: http://localhost:8081 ✅
- **Membership Service**: http://localhost:8082 ✅
- **Frontend**: http://localhost:3000 ✅

### Databases
- **Auth DB**: Port 3307 ✅
- **Gym DB**: Port 3308 ✅
- **Membership DB**: Port 3309 ✅

### Monitoring Stack
- **Prometheus**: http://localhost:9090 ✅
- **Grafana**: http://localhost:3001 ✅
  - Username: `admin`
  - Password: `admin`

## 📊 Access Monitoring Dashboards

### Prometheus (Metrics Database)
```
http://localhost:9090
```
- View raw metrics
- Test PromQL queries
- Check target health

### Grafana (Visualization)
```
http://localhost:3001
```
**First Time Setup:**
1. Login: admin/admin
2. Add Data Source → Prometheus
3. URL: `http://prometheus:9090`
4. Save & Test

## 🚀 Next Step: Install k6

You need to install k6 to run the load tests. Choose one method:

### Method 1: Direct Download (Easiest)
1. Go to: https://github.com/grafana/k6/releases/latest
2. Download: `k6-vX.XX.X-windows-amd64.zip`
3. Extract and add to PATH (or run from extracted folder)

### Method 2: PowerShell (Admin Required)
```powershell
# Open PowerShell as Administrator
choco install k6 -y
# OR
winget install k6 --source winget --accept-source-agreements
```

## 🧪 Run Load Tests (After Installing k6)

### Quick Test (5 minutes)
```powershell
k6 run load-tests/auth-load-test.js
```

### Medium Test (5 minutes)
```powershell
k6 run load-tests/gym-load-test.js
```

### **Full Stress Test (9.5 minutes) - RECOMMENDED**
```powershell
k6 run load-tests/full-system-test.js
```

This will determine if you need Kubernetes!

## 📈 Monitor During Tests

### Watch Container Resources:
```powershell
docker stats
```

### Watch Service Logs:
```powershell
docker-compose logs -f
```

### View in Grafana:
1. Open: http://localhost:3001
2. Create dashboard
3. Add panels with queries like:
   - `rate(http_requests_total[5m])`
   - `http_request_duration_seconds`

## 🎯 What the Tests Will Show

### ✅ Good Results (Docker Compose is Fine)
- Response times < 500ms (p95)
- Error rate < 5%
- All services stable
- CPU/Memory usage normal

### ⚠️ Warning Signs (Consider Optimization)
- Response times 500ms - 1s (p95)
- Error rate 5-10%
- High but stable resource usage

### ❌ Poor Results (Kubernetes Needed)
- Response times > 1s (p95)
- Error rate > 10%
- Container crashes
- Resource exhaustion
- Database connection errors

## 🔧 Troubleshooting

### Services not responding?
```powershell
docker-compose ps
docker-compose logs <service-name>
```

### Need to restart?
```powershell
docker-compose restart
```

### Fresh start?
```powershell
docker-compose down
docker-compose up -d
```

## 📚 Documentation

- Full guide: `LOAD_TESTING_GUIDE.md`
- Test details: `load-tests/README.md`

---

## Ready to Test! 🚀

1. ✅ All services running
2. ✅ Monitoring configured
3. ⏳ Install k6
4. ⏳ Run tests
5. ⏳ Analyze results
6. ⏳ Decision: Kubernetes vs Docker Compose

**Install k6 now and let's see how your system performs!** 💪
