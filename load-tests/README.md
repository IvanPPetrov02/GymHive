# GymHive Load Testing# GymHive Load Testing Guide



Comprehensive load testing framework for GymHive across Docker Compose and Kubernetes deployments.This guide covers performance testing for GymHive across different deployment environments: Docker Compose, Kubernetes, and future autoscaling scenarios.



## 📁 Directory Structure## 📋 Prerequisites



```Install k6:

load-tests/- **Windows (Chocolatey)**: `choco install k6`

├── tests/                    # k6 Test Scripts- **Windows (winget)**: `winget install k6 --source winget`

│   ├── auth-test.js                 # Authentication load test- **macOS (Homebrew)**: `brew install k6`

│   └── full-system-test.js          # Complete system test with scenarios- **Manual**: Download from https://k6.io/docs/get-started/installation/

├── runners/                  # PowerShell Automation

│   ├── docker.ps1                   # Docker Compose test runner## 🧪 Test Scenarios

│   └── kubernetes.ps1               # Kubernetes test runner

├── results/                  # Auto-Generated Results| Test Script | Target Environment | Load Profile | Purpose |

│   └── .gitkeep|-------------|-------------------|--------------|---------|

├── README.md                 # This file| `auth-load-test.js` | Docker/K8s | 200 VUs, 5 min | Test authentication endpoints |

└── QUICK_START.md            # Quick reference guide| `gym-load-test.js` | Docker/K8s | 300 VUs, 5 min | Test gym service CRUD operations |

```| `full-system-test.js` | Docker/K8s | 500 VUs, 9.5 min | End-to-end user scenarios |

| `realistic-load-test.js` | Docker/K8s | Mixed load | Realistic traffic patterns |

## 🚀 Quick Start| `country-scale-test.js` | Docker | 1000 VUs, 10 min | Baseline country-level load |

| `country-scale-peak-test.js` | Docker | 2000 VUs peak | Peak traffic without scaling |

### Docker Compose| `country-scale-peak-test-k8s.js` | Kubernetes | 2000 VUs peak | Peak traffic with K8s load balancing |



```powershell## 🐳 Testing with Docker Compose

cd runners

.\docker.ps1 -TestType auth              # Run authentication testDocker Compose deployment represents a baseline single-instance configuration.

.\docker.ps1 -TestType full-system       # Run full system test

.\docker.ps1 -TestType all               # Run all tests### Setup

``````bash

cd deployment/docker

### Kubernetesdocker-compose up -d

```

```powershell

cd runners### Run Tests

.\kubernetes.ps1 -TestType auth          # Run authentication test```bash

.\kubernetes.ps1 -TestType full-system   # Run full system testcd ../../load-tests

.\kubernetes.ps1 -TestType all           # Run all tests

```# Basic tests

k6 run auth-load-test.js

See **[QUICK_START.md](./QUICK_START.md)** for detailed commands and options.k6 run gym-load-test.js

k6 run full-system-test.js

---

# Country-scale tests

## 📊 Test Descriptionsk6 run country-scale-test.js          # Baseline

k6 run country-scale-peak-test.js     # Peak load

### 1. Authentication Test (`auth-test.js`)```



**Purpose:** Validate authentication service performance under load### Expected Results (Docker Compose)



**Flow:**Results are stored in `results/docker-compose/`:

1. Register new user with unique email

2. Login with credentials**Characteristics:**

3. Verify JWT token received- Single instance per service

- Limited horizontal scaling

**Load Profiles:**- Resource constraints on local machine

| Environment | Peak VUs | Duration | P95 Target |- Good for development and light testing

|-------------|----------|----------|------------|

| Docker | 150 | 5 min | < 2000ms |**Performance Baseline:**

| Kubernetes | 500 | 9 min | < 1500ms |- ✅ Handles 200-500 concurrent users well

- ⚠️ Response times increase at 1000+ users

**Custom Metrics:**- ❌ May fail at 2000+ concurrent users

- `register_duration` - Time to create new user- Database connections become bottleneck

- `login_duration` - Time to authenticate

- `error_rate` - Percentage of failed operationsSee detailed results: [results/docker-compose-results.md](./results/docker-compose-results.md)



---## ☸️ Testing with Kubernetes



### 2. Full System Test (`full-system-test.js`)Kubernetes deployment provides production-like environment with load balancing and multiple replicas.



**Purpose:** Simulate realistic user behavior with multiple scenarios### Setup

```bash

**Scenarios:**cd deployment/kubernetes

.\deploy-k8s.ps1

1. **Browse Gyms (30% of traffic)**

   - Anonymous users viewing gym list# Verify deployment

   - Tests pagination and filteringkubectl get pods -n gymhive

kubectl get services -n gymhive

2. **Authentication (25% of traffic)**```

   - New users registering

   - Existing users logging in### Update Test Configuration



3. **Gym Details (25% of traffic)**Edit test scripts to use Kubernetes NodePort URLs:

   - Viewing specific gym information- API Gateway: `http://localhost:30000`

   - Reading reviews and ratings- Frontend: `http://localhost:30003`



4. **Full User Journey (20% of traffic)**### Run Tests

   - Complete flow: browse → register → login → view details```bash

   - Most resource-intensive scenariocd ../../load-tests



**Load Profiles:**# Basic tests with K8s

| Environment | Peak VUs | Duration | P95 Target |k6 run auth-load-test.js

|-------------|----------|----------|------------|k6 run gym-load-test.js

| Docker | 150 | 6 min | < 2500ms |k6 run full-system-test.js

| Kubernetes | 700 | 11 min | < 1500ms |

# Country-scale test for K8s

**What It Tests:**k6 run country-scale-peak-test-k8s.js

- End-to-end system integration```

- Service-to-service communication

- Database performance under mixed load### Expected Results (Kubernetes)

- API Gateway routing and load balancing

Results are stored in `results/kubernetes/`:

---

**Characteristics:**

## 🔧 Prerequisites- Multiple pod replicas (2-3 per service)

- Kubernetes service load balancing

### Required Software- Better resource distribution

- Automatic pod recovery

1. **k6 Load Testing Tool**

   ```powershell**Performance Improvements:**

   # Windows (Chocolatey)- ✅ Handles 500-1000 concurrent users consistently

   choco install k6- ✅ Better response times under load

   - ✅ Lower error rates due to load distribution

   # Windows (winget)- ✅ Graceful degradation under extreme load

   winget install k6 --source winget

   See detailed results: [results/kubernetes-results.md](./results/kubernetes-results.md)

   # macOS (Homebrew)

   brew install k6## 🔄 Future: Kubernetes with Horizontal Pod Autoscaling (HPA)

   ```

The next evolution will add automatic scaling based on resource utilization.

2. **PowerShell** (pre-installed on Windows)

### Setup (Coming Soon)

3. **Docker Desktop** (for Docker tests)

```bash

4. **kubectl + Kubernetes cluster** (for K8s tests)# Enable metrics-server (already deployed)

kubectl get deployment metrics-server -n kube-system

### Service Requirements

# Apply HPA configuration

- **Docker:** Services must be running via `deployment/docker/docker-compose.yml`kubectl apply -f deployment/kubernetes/hpa/

- **Kubernetes:** Services deployed to K8s cluster with namespace (default: `gymhive`)

- **Health Checks:** API Gateway must respond to `/health` endpoint# Verify HPA

kubectl get hpa -n gymhive

---```



## 📈 Understanding Results### HPA Configuration



### Result Files```yaml

# Example HPA for auth-service

After each test run, two files are auto-generated in `results/`:apiVersion: autoscaling/v2

kind: HorizontalPodAutoscaler

```metadata:

docker-auth-comprehensive-test-2025-11-10T22-30-45.json      # Raw metrics  name: auth-service-hpa

docker-auth-comprehensive-test-2025-11-10T22-30-45.md        # Human-readable reportspec:

```  scaleTargetRef:

    apiVersion: apps/v1

**Naming Convention:**      kind: Deployment

`{environment}-{test-name}-{timestamp}.{format}`    name: auth-service

  minReplicas: 2

### Key Metrics  maxReplicas: 10

  metrics:

| Metric | Description | Target |  - type: Resource

|--------|-------------|--------|    resource:

| **HTTP Success Rate** | % of successful requests | > 99% |      name: cpu

| **P95 Response Time** | 95th percentile latency | < threshold |      target:

| **Error Rate** | % of failed operations | < 1% |        type: Utilization

| **Checks Passed** | Assertion success rate | 100% |        averageUtilization: 70

| **Throughput** | Requests per second | Varies |  - type: Resource

    resource:

### Performance Thresholds      name: memory

      target:

Tests automatically fail if thresholds are exceeded:        type: Utilization

        averageUtilization: 80

**Docker Compose:**```

- ✅ P95 < 2000ms (auth test)

- ✅ P95 < 2500ms (full system test)### Testing HPA

- ✅ Error rate = 0%

- ✅ Success rate = 100%```bash

# Monitor scaling in real-time

**Kubernetes:**kubectl get hpa -n gymhive -w

- ✅ P95 < 1500ms (both tests)

- ✅ Error rate < 0.5%# Run peak load test

- ✅ Success rate > 99.5%k6 run country-scale-peak-test-k8s.js



### Interpreting Results# Watch pods scale up

kubectl get pods -n gymhive -w

**✅ Successful Test:**```

```

✓ http_req_failed.......rate: 0.00%### Expected Results (Kubernetes + HPA)

✓ http_req_duration....p(95): 1456.32ms

✓ checks...............rate: 100.00%Results will be stored in `results/kubernetes-hpa/`:

```

**Characteristics:**

**❌ Failed Test:**- Dynamic scaling (2-10 pods per service)

```- CPU/Memory threshold-based scaling

✗ http_req_failed.......rate: 5.23%     ← Too many errors- Automatic scale-down after load decreases

✗ http_req_duration....p(95): 3245.67ms  ← Exceeds threshold- Cost-efficient resource utilization

✗ checks...............rate: 94.52%      ← Failed assertions

```**Performance Goals:**

- ✅ Handle 2000+ concurrent users

**Common Issues:**- ✅ Maintain p95 response times <500ms

- **High error rate:** Service crashes, database connections exhausted- ✅ <1% error rate even at peak

- **Slow response times:** CPU/memory limits, inefficient queries- ✅ Automatic recovery from traffic spikes

- **Failed checks:** Unexpected API responses, authentication issues- ✅ Cost optimization through scale-down



---See future results: [results/kubernetes-hpa-results.md](./results/kubernetes-hpa-results.md)



## 🎯 Testing Strategy## 📊 Understanding Test Results



### 1. Baseline with Docker### Key Metrics



Start with Docker Compose to establish baseline performance:| Metric | Description | Good | Warning | Critical |

|--------|-------------|------|---------|----------|

```powershell| `http_req_duration` (p95) | 95% of requests complete in | <500ms | 500-1000ms | >1000ms |

cd runners| `http_req_duration` (p99) | 99% of requests complete in | <1000ms | 1000-2000ms | >2000ms |

.\docker.ps1 -TestType all| `http_req_failed` | Failed request rate | <1% | 1-5% | >5% |

```| `http_reqs` | Requests per second | - | - | - |

| `iterations` | Completed scenarios | Higher is better | - | - |

**Expected Outcome:**| `vus` | Virtual users | Follows test stages | - | - |

- Auth test: ~50 req/s, P95 < 2000ms

- Full system: ~30 req/s, P95 < 2500ms### Interpreting Results

- 100% success rate

**✅ System is Healthy:**

### 2. Scale with Kubernetes- p95 response time <500ms

- Error rate <1%

Deploy to Kubernetes and compare:- No timeout errors

- Stable resource usage

```powershell

cd runners**⚠️ System is Stressed:**

.\kubernetes.ps1 -TestType all- p95 response time 500-1000ms

```- Error rate 1-5%

- Some timeout errors

**Expected Improvements:**- Rising resource usage

- Higher throughput (3-5x)

- Lower P95 latency (30-40% reduction)**❌ System is Overloaded:**

- Better resilience under peak load- p95 response time >1000ms

- Error rate >5%

### 3. Compare Results- Many timeout/connection errors

- Resource exhaustion (CPU/Memory at 100%)

Review markdown files in `results/` to compare:

- Response time distributions## 🔍 Monitoring During Tests

- Error rates under different loads

- Resource utilization patterns### Docker Compose Monitoring



---```bash

# Real-time container stats

## 🛠️ Customizationdocker stats



### Adjusting Load Profiles# Service logs

docker-compose logs -f auth-service

Edit test files in `tests/` to modify load:docker-compose logs -f gym-service

docker-compose logs -f membership-service

```javascript

// In auth-test.js or full-system-test.js# Grafana dashboards

http://localhost:3001

const configs = {# Login: admin/admin

  docker: {# Dashboard: Microservices Overview

    stages: [```

      { duration: '30s', target: 50 },    // Ramp up

      { duration: '2m', target: 100 },    // Increase load### Kubernetes Monitoring

      { duration: '1m', target: 150 },    // Peak - MODIFY THIS

      { duration: '30s', target: 0 },     // Ramp down```bash

    ],# Pod resource usage

    thresholds: {kubectl top pods -n gymhive

      'http_req_duration{scenario:auth}': ['p(95)<2000'],  // MODIFY THIS

    },# Node resource usage

  },kubectl top nodes

  // ...

};# Pod logs

```kubectl logs -f deployment/auth-service -n gymhive

kubectl logs -f deployment/gym-service -n gymhive

### Adding Custom Metrics

# Grafana dashboards

```javascripthttp://localhost:30030

import { Trend } from 'k6/metrics';# Login: admin/admin

# Dashboard: Kubernetes Monitoring

const customMetric = new Trend('my_custom_duration');```



export default function() {### Prometheus Metrics

  const start = Date.now();

  // ... your code ...Access Prometheus:

  customMetric.add(Date.now() - start);- Docker: http://localhost:9090

}- Kubernetes: http://localhost:30090

```

**Useful Queries:**

### Creating New Tests```promql

# Request rate per service

1. Copy `tests/auth-test.js` as templaterate(http_requests_total[5m])

2. Modify scenarios and load profiles

3. Update `runners/*.ps1` to include new test# 95th percentile latency

4. Run with `.\docker.ps1 -TestType your-new-test`histogram_quantile(0.95, rate(http_request_duration_seconds_bucket[5m]))



---# Error rate

rate(http_requests_total{status=~"5.."}[5m]) / rate(http_requests_total[5m])

## 📚 Additional Resources

# Pod CPU usage (K8s only)

- **k6 Documentation:** https://k6.io/docs/rate(container_cpu_usage_seconds_total{namespace="gymhive"}[5m])

- **Best Practices:** https://k6.io/docs/testing-guides/test-types/```

- **Metrics Reference:** https://k6.io/docs/using-k6/metrics/

## 📈 Comparing Environments

---

### Docker Compose vs Kubernetes

## 🐛 Troubleshooting

| Aspect | Docker Compose | Kubernetes | K8s + HPA |

### "API Gateway is not responding"|--------|----------------|------------|-----------|

| **Max Users** | 500-1000 | 1000-2000 | 2000+ |

**Docker:**| **Response Time** | Good up to 500 | Good up to 1500 | Consistent |

```powershell| **Scalability** | Manual only | Manual + some auto | Fully automatic |

docker ps                    # Check services are running| **Recovery** | Manual restart | Auto-restart pods | Auto-scale + restart |

docker logs api-gateway      # Check gateway logs| **Cost** | Lowest | Medium | Optimized |

```| **Complexity** | Lowest | Medium | Highest |

| **Best For** | Dev/testing | Production (small) | Production (variable load) |

**Kubernetes:**

```powershell## 🎯 Test Execution Workflow

kubectl get pods -n gymhive         # Check pod status

kubectl logs deployment/api-gateway # Check logs### 1. Baseline Testing (Docker Compose)

``````bash

cd deployment/docker

### "k6 command not found"docker-compose up -d

cd ../../load-tests

Install k6:k6 run country-scale-test.js > results/docker-baseline.txt

```powershell```

choco install k6

# or### 2. Peak Load Testing (Docker Compose)

winget install k6 --source winget```bash

```k6 run country-scale-peak-test.js > results/docker-peak.txt

# Analyze: Can Docker Compose handle peak load?

### "Results not saving"```



Check file paths in test scripts point to `../results/` (relative to `tests/` folder).### 3. Kubernetes Testing

```bash

### High Error Ratescd ../deployment/kubernetes

.\deploy-k8s.ps1

1. Check service health before testingcd ../../load-tests

2. Reduce peak VU count in test configk6 run country-scale-peak-test-k8s.js > results/k8s-peak.txt

3. Increase service resource limits# Compare: Is K8s performing better?

4. Review application logs for errors```



---### 4. Future: HPA Testing

```bash

## 📊 Example Resultskubectl apply -f ../deployment/kubernetes/hpa/

k6 run country-scale-peak-test-k8s.js

### Docker Compose Baseline# Monitor: Watch pods scale automatically

kubectl get hpa -n gymhive -w

``````

Environment: Docker Compose (single instance per service)

Test: auth-comprehensive-test## 📁 Results Directory Structure

Duration: 4m 1s

Peak VUs: 150```

results/

Results:├── docker-compose-results.md       # Docker Compose test results

  Total Requests:    12,316├── kubernetes-results.md           # Kubernetes test results  

  Request Rate:      51.11 req/s├── kubernetes-hpa-results.md       # Future: K8s with HPA results

  Success Rate:      100.00%├── comparison-analysis.md          # Environment comparison

  Error Rate:        0.00%├── docker/                         # Raw Docker test outputs

├── kubernetes/                     # Raw K8s test outputs

Response Times:└── kubernetes-hpa/                 # Future: Raw HPA test outputs

  Average:  1,245.32ms```

  Median:   1,156.78ms

  P95:      2,190.45ms  ✅ Under threshold## 🛠️ Troubleshooting

  P99:      2,845.12ms

### High Error Rates

Recommendation: Suitable for < 100 concurrent users

```**Docker Compose:**

- Increase container resources

### Kubernetes Scaled- Check database connection pool size

- Review service logs

```

Environment: Kubernetes (3 replicas per service)**Kubernetes:**

Test: auth-comprehensive-test  - Increase replica count

Duration: 9m 15s- Check pod resource limits

Peak VUs: 500- Verify service endpoints



Results:### Slow Response Times

  Total Requests:    45,234

  Request Rate:      81.47 req/s- Check database query performance

  Success Rate:      99.98%- Review Grafana dashboards for bottlenecks

  Error Rate:        0.02%- Enable database connection pooling

- Add caching layer

Response Times:

  Average:  845.23ms### Connection Timeouts

  Median:   756.12ms

  P95:      1,324.67ms  ✅ Under threshold- Increase timeout values in test scripts

  P99:      1,867.34ms- Check network connectivity

- Verify service health endpoints

Recommendation: Suitable for 200-400 concurrent users

```## 📚 Additional Resources



---- [k6 Documentation](https://k6.io/docs/)

- [Prometheus Query Examples](https://prometheus.io/docs/prometheus/latest/querying/examples/)

## 📝 License- [Kubernetes HPA](https://kubernetes.io/docs/tasks/run-application/horizontal-pod-autoscale/)

- [Grafana Dashboards](https://grafana.com/docs/grafana/latest/dashboards/)

This load testing framework is part of the GymHive project.

---

**Note:** This is a living document. Results will be updated as tests are conducted across different environments.