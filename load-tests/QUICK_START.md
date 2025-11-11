# Load Testing Quick Start Guide

## 📁 Folder Structure

```
load-tests/
├── tests/              # Test scripts (k6 JavaScript)
│   ├── auth-test.js           # Authentication load test
│   └── full-system-test.js    # Complete system test
├── runners/            # PowerShell automation scripts
│   ├── docker.ps1             # Run tests on Docker Compose
│   └── kubernetes.ps1         # Run tests on Kubernetes
├── results/            # Auto-generated test results (timestamped)
└── README.md           # Comprehensive documentation
```

## 🚀 Quick Commands

### Docker Compose Testing

```powershell
# Run authentication test
cd load-tests/runners
.\docker.ps1 -TestType auth

# Run full system test
.\docker.ps1 -TestType full-system

# Run all tests
.\docker.ps1 -TestType all

# Run quietly (minimal output)
.\docker.ps1 -TestType auth -Quiet
```

### Kubernetes Testing

```powershell
# Run authentication test
cd load-tests/runners
.\kubernetes.ps1 -TestType auth

# Run full system test  
.\kubernetes.ps1 -TestType full-system

# Run all tests
.\kubernetes.ps1 -TestType all

# Specify namespace (default: gymhive)
.\kubernetes.ps1 -TestType auth -Namespace mynamespace
```

## 📊 Test Profiles

### Authentication Test (`auth-test.js`)

**Docker Compose:**
- **Target Load:** 150 peak VUs
- **Duration:** ~5 minutes
- **Thresholds:** P95 < 2000ms, 0% errors

**Kubernetes:**
- **Target Load:** 500 peak VUs
- **Duration:** ~9 minutes
- **Thresholds:** P95 < 1500ms, 0% errors

**What it tests:**
- User registration with unique emails
- User login with JWT authentication
- Response times and success rates
- Custom metrics: register_duration, login_duration

---

### Full System Test (`full-system-test.js`)

**Docker Compose:**
- **Target Load:** 150 peak VUs
- **Duration:** ~6 minutes
- **Thresholds:** P95 < 2500ms, 0% errors

**Kubernetes:**
- **Target Load:** 700 peak VUs
- **Duration:** ~11 minutes
- **Thresholds:** P95 < 1500ms, 0% errors

**What it tests:**
- 4 realistic scenarios:
  1. **Browse Gyms** - Anonymous users browsing
  2. **Authentication** - Register + login flow
  3. **Gym Details** - Viewing specific gym info
  4. **Full User Journey** - Complete user experience
- Random scenario selection per VU
- End-to-end system integration

## 📈 Result Files

After each test run, results are automatically saved to `results/` with timestamps:

```
results/
├── docker-auth-comprehensive-test-2025-11-10T22-30-45.json
├── docker-auth-comprehensive-test-2025-11-10T22-30-45.md
├── docker-full-system-test-2025-11-10T22-37-15.json
└── docker-full-system-test-2025-11-10T22-37-15.md
```

- **`.json`** - Raw k6 metrics and data
- **`.md`** - Formatted report with analysis

## 🔍 Interpreting Results

### Key Metrics

- **Success Rate:** Should be 100% (or very close)
- **P95 Response Time:** 95th percentile - most users experience this or better
- **Error Rate:** Should be 0% or very low
- **Checks Passed:** All checks should pass (100%)

### Performance Targets

| Environment | P95 Target | Peak VUs | Expected Success |
|-------------|-----------|----------|------------------|
| Docker | < 2000ms | 150 | 100% |
| Kubernetes | < 1500ms | 500-700 | 100% |

### When to Worry

- ❌ **Success rate < 99%** → Service errors or timeouts
- ❌ **P95 > threshold** → Performance degradation under load
- ❌ **Error rate > 1%** → Application or infrastructure issues
- ❌ **Failed checks** → Unexpected API responses

## 🛠️ Prerequisites

1. **k6 installed:** [Download k6](https://k6.io/docs/get-started/installation/)
2. **Docker running** (for Docker tests)
3. **Kubernetes cluster** (for K8s tests)
4. **Services deployed and healthy**

## 💡 Tips

- Start with **Docker tests** to establish baseline performance
- Use **`-Quiet`** flag when running multiple tests to reduce noise
- Check **`results/`** folder for detailed reports after each run
- Compare **Docker vs Kubernetes** results to validate scaling benefits
- Monitor resource usage during tests:
  - Docker: `docker stats`
  - Kubernetes: `kubectl top pods -n gymhive`
