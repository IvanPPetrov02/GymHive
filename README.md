# GymHive

[![Frontend CI](https://github.com/IvanPPetrov02/GymHive/actions/workflows/FrontEndCI.yml/badge.svg?branch=dev)](https://github.com/IvanPPetrov02/GymHive/actions/workflows/FrontEndCI.yml)
[![Authentication Service CI](https://github.com/IvanPPetrov02/GymHive/actions/workflows/AuthenticationServiceCI.yml/badge.svg?branch=dev)](https://github.com/IvanPPetrov02/GymHive/actions/workflows/AuthenticationServiceCI.yml)
[![Gym Service CI](https://github.com/IvanPPetrov02/GymHive/actions/workflows/GymServiceCI.yml/badge.svg?branch=dev)](https://github.com/IvanPPetrov02/GymHive/actions/workflows/GymServiceCI.yml)
[![Membership Service CI](https://github.com/IvanPPetrov02/GymHive/actions/workflows/MembershipServiceCI.yml/badge.svg?branch=dev)](https://github.com/IvanPPetrov02/GymHive/actions/workflows/MembershipServiceCI.yml)
[![API Gateway CI](https://github.com/IvanPPetrov02/GymHive/actions/workflows/ApiGatewayCI.yml/badge.svg?branch=dev)](https://github.com/IvanPPetrov02/GymHive/actions/workflows/ApiGatewayCI.yml)

Cloud-native microservices platform for gym management with membership systems, group management, and role-based administration.

## 🚀 Quick Start

### Using Docker (Recommended)

```bash
# Pull latest images from Docker Hub and start all services
cd deployment/docker
docker-compose pull
docker-compose up -d

# Access the application
# Frontend:         http://localhost:3000
# API Gateway:      http://localhost:5000
# Auth Service:     http://localhost:8080
# Gym Service:      http://localhost:8081
# Membership Service: http://localhost:8082
# Prometheus:       http://localhost:9090
# Grafana:          http://localhost:3001
# Auth Database:    localhost:3307
# Gym Database:     localhost:3308
# Membership Database: localhost:3309
```

**Note**: Docker Compose now uses pre-built images from [Docker Hub](https://hub.docker.com/u/ivanppetrov) - no local build required!

### Local Development

```bash
# Start databases only
docker-compose up -d auth-db gym-db

# Terminal 1: Run API Gateway
cd GymHiveBackend/ApiGateway
dotnet run

# Terminal 2: Run Authentication Service
cd GymHiveBackend/AuthenticationService
dotnet run

# Terminal 3: Run Gym Service
cd GymHiveBackend/GymService
dotnet run

# Terminal 4: Run Frontend
cd GymHiveFrontend
npm install
npm run dev
```

## 🐳 Deployment Options

### Docker Compose (Development & Testing)

Docker Compose provides a quick way to run all services locally with monitoring:

```bash
cd deployment/docker
docker-compose up -d
```

**What it includes:**
- All 3 microservices (Auth, Gym, Membership)
- API Gateway with YARP reverse proxy
- Frontend (Svelte SPA)
- 3 MySQL databases (one per service)
- Prometheus for metrics collection
- Grafana with pre-configured dashboards

**Access URLs:**
- Application: http://localhost:3000
- Grafana: http://localhost:3001 (admin/admin)
- Prometheus: http://localhost:9090

### Kubernetes (Production-like Environment)

Kubernetes deployment provides a production-ready setup with service discovery, load balancing, and horizontal scaling capabilities:

```bash
cd deployment/kubernetes
.\deploy-k8s.ps1
```

**What it includes:**
- All microservices deployed as Deployments
- MySQL StatefulSets with persistent storage
- Prometheus operator for metrics
- Grafana with Kubernetes-specific dashboards
- kube-state-metrics for cluster metrics
- metrics-server for resource metrics

**Access URLs:**
- Application: http://localhost:30000 (NodePort)
- Grafana: http://localhost:30030 (admin/admin)
- Prometheus: http://localhost:30090

**Kubernetes Features:**
- Service discovery via DNS
- Load balancing across pod replicas
- Rolling updates with zero downtime
- Resource limits and requests
- Horizontal Pod Autoscaling (HPA) ready
- Persistent volume claims for databases

### When to Use Each

| Scenario | Docker Compose | Kubernetes |
|----------|----------------|------------|
| Local development | ✅ Recommended | ❌ Overkill |
| Testing changes quickly | ✅ Fast startup | ⚠️ Slower |
| Integration testing | ✅ Good | ✅ Better |
| Load testing | ✅ Basic | ✅ Production-like |
| Production simulation | ⚠️ Limited | ✅ Recommended |
| Learning K8s concepts | ❌ N/A | ✅ Perfect |
| CI/CD pipelines | ✅ Simple | ✅ Advanced |

## 📊 Monitoring with Prometheus & Grafana

### Prometheus

Prometheus is configured to scrape metrics from all services automatically:

**Docker Compose**: Uses file-based service discovery
- Config: `deployment/monitoring-docker/prometheus/prometheus.yml`
- Service discovery files: `deployment/monitoring-docker/prometheus/file_sd/*.yml`

**Kubernetes**: Uses Kubernetes service discovery
- Config: `deployment/kubernetes/monitoring-k8s/prometheus.yaml`
- Auto-discovers pods with prometheus.io annotations

**Available Metrics:**
- HTTP request rates, durations, and status codes
- .NET runtime metrics (GC, thread pool, exceptions)
- Database connection pool metrics
- Custom business metrics

**Useful Queries:**
```promql
# Request rate per service
rate(http_requests_total[5m])

# 95th percentile latency
histogram_quantile(0.95, rate(http_request_duration_seconds_bucket[5m]))

# Error rate
rate(http_requests_total{status=~"5.."}[5m])
```

### Grafana Dashboards

Three pre-built dashboards are included:

1. **Microservices Overview** - High-level service health
   - Request rates and error rates per service
   - Response time percentiles (p50, p95, p99)
   - Active requests and throughput

2. **Container Metrics** - Resource utilization
   - CPU and memory usage per container
   - Network I/O
   - Disk usage

3. **Kubernetes Monitoring** - Cluster-level metrics
   - Pod status and restarts
   - Node resource usage
   - Deployment health
   - Persistent volume claims

**Default Credentials**: admin/admin (change on first login)

**Dashboard Locations:**
- Docker: `deployment/monitoring-docker/grafana/provisioning/dashboards/`
- K8s: Auto-provisioned from ConfigMaps

## 🏗️ Architecture

This project follows a **microservices architecture** with an API Gateway pattern:

```
   ┌─────────────────┐
   │   Frontend      │
   │  (Svelte SPA)   │
   └────────┬────────┘
            │
            ▼
   ┌─────────────────────┐
   │  API Gateway        │
   │   (YARP Proxy)      │
   └────┬────────┬─────┬─┘
        │        │     │
        ▼        ▼     ▼
┌────────┐ ┌─────────┐ ┌──────────────┐
│  Auth  │ │   Gym   │ │ Membership   │
│Service │ │ Service │ │   Service    │
└───┬────┘ └────┬────┘ └───────┬──────┘
    │           │              │
    ▼           ▼              ▼
┌────────┐ ┌─────────┐ ┌──────────────┐
│Auth DB │ │ Gym DB  │ │Membership DB │
│(MySQL) │ │ (MySQL) │ │   (MySQL)    │
└────────┘ └─────────┘ └──────────────┘
```

### Services
- **Frontend**: Svelte 5 + TypeScript + Vite + TailwindCSS
- **API Gateway**: ASP.NET Core YARP reverse proxy with authentication
- **Authentication Service**: JWT-based user authentication and management
- **Gym Service**: Gyms and gym groups management
- **Membership Service**: Membership management and operations
- **Databases**: Separate MySQL 8.0 databases per service (database-per-service pattern)

## 🚀 CI/CD Pipelines

Each component has its own CI pipeline that only runs when relevant files are changed:

| Component | Status | Triggers On |
|-----------|--------|-------------|
| Frontend | [![Frontend CI](https://github.com/IvanPPetrov02/GymHive/actions/workflows/FrontEndCI.yml/badge.svg?branch=dev)](https://github.com/IvanPPetrov02/GymHive/actions/workflows/FrontEndCI.yml) | Changes in `GymHiveFrontend/**` |
| Authentication Service | [![Auth CI](https://github.com/IvanPPetrov02/GymHive/actions/workflows/AuthenticationServiceCI.yml/badge.svg?branch=dev)](https://github.com/IvanPPetrov02/GymHive/actions/workflows/AuthenticationServiceCI.yml) | Changes in `GymHiveBackend/AuthenticationService/**` |
| Gym Service | [![Gym CI](https://github.com/IvanPPetrov02/GymHive/actions/workflows/GymServiceCI.yml/badge.svg?branch=dev)](https://github.com/IvanPPetrov02/GymHive/actions/workflows/GymServiceCI.yml) | Changes in `GymHiveBackend/GymService/**` |
| Membership Service | [![Membership CI](https://github.com/IvanPPetrov02/GymHive/actions/workflows/MembershipServiceCI.yml/badge.svg?branch=dev)](https://github.com/IvanPPetrov02/GymHive/actions/workflows/MembershipServiceCI.yml) | Changes in `GymHiveBackend/MembershipService/**` |
| API Gateway | [![Gateway CI](https://github.com/IvanPPetrov02/GymHive/actions/workflows/ApiGatewayCI.yml/badge.svg?branch=dev)](https://github.com/IvanPPetrov02/GymHive/actions/workflows/ApiGatewayCI.yml) | Changes in `GymHiveBackend/ApiGateway/**` |

## 📦 Project Structure

```
GymHive/
├── GymHiveFrontend/              # Svelte SPA Frontend
│   ├── src/
│   │   ├── lib/
│   │   │   ├── pages/            # Application pages
│   │   │   │   ├── admin/        # Admin CRUD pages (Gyms, Users, Groups)
│   │   │   │   └── moderator/    # Moderator pages (Members, Memberships)
│   │   │   ├── components/       # Reusable UI components
│   │   │   │   ├── ui/           # UI widgets (Modal, Toast, etc.)
│   │   │   │   └── Navbar.svelte
│   │   │   └── services/         # API service layer
│   │   │       ├── api.ts        # Base API configuration
│   │   │       ├── auth.ts       # Authentication service
│   │   │       ├── gyms.ts       # Gym management
│   │   │       ├── memberships.ts
│   │   │       ├── gymGroups.ts
│   │   │       └── users.ts
│   │   └── App.svelte
│   ├── Dockerfile
│   └── package.json
│
├── GymHiveBackend/
│   ├── ApiGateway/               # YARP Reverse Proxy
│   │   ├── Program.cs
│   │   ├── appsettings.json      # Route configuration
│   │   └── Middleware/           # JWT validation
│   │
│   ├── AuthenticationService/    # User Authentication Microservice
│   │   ├── AuthenticationController.cs
│   │   ├── BLL/                  # Business Logic Layer
│   │   │   ├── Managers/         # User management
│   │   │   ├── Services/         # JWT service
│   │   │   ├── DTOs/             # Data Transfer Objects
│   │   │   └── Entities/         # User, Role entities
│   │   ├── DAL/                  # Data Access Layer
│   │   │   ├── DbContexts/
│   │   │   └── Repositories/
│   │   ├── Tests/                # Unit tests
│   │   └── Dockerfile
│   │
│   ├── GymService/               # Gym Management Microservice
│   │   ├── Controllers/
│   │   │   ├── GymsController.cs
│   │   │   ├── GymGroupsController.cs
│   │   │   └── MembershipsController.cs
│   │   ├── BLL/                  # Business Logic Layer
│   │   │   ├── Managers/
│   │   │   ├── DTOs/
│   │   │   └── Entities/         # Gym, GymGroup, Membership entities
│   │   ├── DAL/                  # Data Access Layer
│   │   ├── Tests/
│   │   └── Dockerfile
│   │
│   └── MembershipService/        # Membership Management Microservice
│       ├── Controllers/
│       ├── BLL/
│       ├── DAL/
│       └── Dockerfile
│
├── deployment/                   # Deployment configurations
│   ├── docker/
│   │   └── docker-compose.yml    # Docker Compose setup
│   ├── kubernetes/               # Kubernetes manifests
│   │   ├── services/             # Service deployments
│   │   ├── databases/            # Database StatefulSets
│   │   ├── monitoring-k8s/       # Prometheus & Grafana for K8s
│   │   └── deploy-k8s.ps1        # Deployment script
│   └── monitoring-docker/        # Monitoring for Docker Compose
│       ├── prometheus/
│       │   ├── prometheus.yml
│       │   └── file_sd/          # Service discovery
│       └── grafana/
│           └── provisioning/
│               ├── datasources/
│               └── dashboards/   # Pre-built dashboards
│
├── load-tests/                   # Performance testing
│   ├── auth-load-test.js
│   ├── gym-load-test.js
│   ├── full-system-test.js
│   └── results/
│
├── docs/                         # Documentation
│   └── CI_CD_SETUP_GUIDE.md
│
├── .github/workflows/            # CI/CD Pipelines
│   ├── FrontEndCI.yml
│   ├── AuthenticationServiceCI.yml
│   ├── GymServiceCI.yml
│   ├── MembershipServiceCI.yml
│   └── ApiGatewayCI.yml
│
├── docker-compose.yml            # Production Docker Compose
├── .env.example                  # Environment variables template
├── GymHive.sln                   # .NET solution file
└── README.md
```

## 🛠️ Technologies

### Frontend
- **Framework**: Svelte 5 + TypeScript
- **Build Tool**: Vite 7
- **Styling**: TailwindCSS 4
- **Routing**: svelte-spa-router 4
- **State Management**: Svelte stores
- **HTTP Client**: Fetch API with custom service layer

### Backend
- **Framework**: ASP.NET Core 9.0
- **API Gateway**: YARP (Yet Another Reverse Proxy)
- **Database**: MySQL 8.0 with Entity Framework Core
- **Authentication**: JWT Bearer tokens with role-based authorization
- **API Documentation**: Swagger/OpenAPI (per service)
- **Architecture**: 
  - Clean Architecture (BLL/DAL separation)
  - Microservices pattern
  - Database-per-service pattern
  - API Gateway pattern

### DevOps
- **Containerization**: Docker + Docker Compose
- **Orchestration**: Kubernetes support with manifests
- **CI/CD**: GitHub Actions (separate pipelines per service)
- **Monitoring**: Prometheus + Grafana with pre-built dashboards
- **Service Discovery**: File-based service discovery for Prometheus
- **Metrics**: prometheus-net for .NET services
- **Load Testing**: k6 performance tests
- **Version Control**: Git + GitHub

## 🎨 Frontend Pages

### Public Pages
- **Home** (`/`) - Landing page
- **Login/Register** (`/login`, `/register`) - Authentication

### User Pages (Authentication Required)
- **Find Gyms** (`/gyms`) - Browse all gyms
- **Gym Details** (`/gyms/:id`) - View gym info, facilities, groups, purchase memberships
- **Profile** (`/profile`) - 4-tab interface:
  - View Profile - Display user information
  - Edit Profile - Update personal details
  - Change Password - Security management
  - My Memberships - View and cancel memberships

### Admin Pages (Admin Role Required)
- **Gyms Management** (`/admin/gyms`) - Full CRUD for gyms
- **Users Management** (`/admin/users`) - User role management, activate/deactivate
- **Gym Groups Management** (`/admin/groups`) - Create groups, assign moderators

### Moderator Pages (Moderator/Admin Role Required)
- **Group Members** (`/moderator/members`) - Manage members in moderated groups
- **Memberships Management** (`/moderator/memberships`) - Manage memberships for moderated groups

### Frontend Features
- **Role-based access control** (User, Moderator, Admin)
- **Route guards** with automatic redirects
- **Reusable UI components** (LoadingSpinner, Modal, ConfirmDialog, Toast)
- **API service layer** for clean separation of concerns
- **Global toast notifications** for user feedback
- **Responsive design** for mobile and desktop
- **Form validation** with loading states
- **Optimistic UI updates** with error handling

## � API Endpoints

### Authentication Service (`/api/authentication`)
| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| POST | `/register` | Register new user | ❌ |
| POST | `/login` | Login and get JWT token | ❌ |
| GET | `/{uuid}` | Get user by ID | ✅ |
| PUT | `/{uuid}` | Update user details | ✅ |
| DELETE | `/{uuid}` | Delete user account | ✅ |
| PUT | `/{uuid}/password` | Change user password | ✅ |
| GET | `/GetUser` | Get current authenticated user | ✅ |
| POST | `/validate-token` | Validate JWT token | ✅ |

### Gym Service - Gyms (`/api/gyms`)
| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| GET | `/` | Get all gyms | ✅ |
| GET | `/{id}` | Get gym by ID | ✅ |
| POST | `/` | Create new gym | ✅ Admin |
| PUT | `/{id}` | Update gym | ✅ Admin |
| DELETE | `/{id}` | Delete gym | ✅ Admin |

### Gym Service - Gym Groups (`/api/gymgroups`)
| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| GET | `/` | Get all gym groups | ✅ |
| GET | `/{id}` | Get group by ID | ✅ |
| GET | `/gym/{gymId}` | Get groups by gym | ✅ |
| GET | `/moderator/{moderatorId}` | Get groups by moderator | ✅ |
| GET | `/{id}/members` | Get group members | ✅ |
| POST | `/` | Create new group | ✅ Admin |
| POST | `/{id}/members` | Add member to group | ✅ Moderator |
| PUT | `/{id}` | Update group | ✅ Admin |
| DELETE | `/{id}` | Delete group | ✅ Admin |
| DELETE | `/{id}/members/{userId}` | Remove member | ✅ Moderator |

### Gym Service - Memberships (`/api/memberships`)
| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| GET | `/` | Get all memberships | ✅ Admin |
| GET | `/{id}` | Get membership by ID | ✅ |
| GET | `/user/{userId}` | Get user's memberships | ✅ |
| GET | `/my-memberships` | Get current user's memberships | ✅ |
| GET | `/gym/{gymId}` | Get gym's memberships | ✅ Moderator |
| POST | `/` | Purchase membership | ✅ |
| PUT | `/{id}` | Update membership | ✅ Moderator |
| DELETE | `/{id}` | Cancel membership | ✅ |

### API Gateway (`http://localhost:5000`)
All requests go through the API Gateway which handles:
- JWT token validation
- Request routing to appropriate services
- CORS configuration
- Health checks at `/health`

## 📚 Documentation

- **Deployment**:
  - [Docker Deployment](./deployment/docker/docker-compose.yml) - Docker Compose configuration
  - [Kubernetes Deployment](./deployment/kubernetes/) - K8s manifests and deployment scripts
- **Monitoring**:
  - Prometheus: `http://localhost:9090` (Docker) or `http://localhost:30090` (Kubernetes)
  - Grafana: `http://localhost:3001` (Docker) or `http://localhost:30030` (Kubernetes)
  - Pre-built dashboards: Microservices Overview, Container Metrics, Kubernetes Monitoring
- **CI/CD**: [CI/CD Setup Guide](./docs/CI_CD_SETUP_GUIDE.md)
- **API Documentation**:
  - Auth Service Swagger: `http://localhost:8080/swagger`
  - Gym Service Swagger: `http://localhost:8081/swagger`
  - Membership Service Swagger: `http://localhost:8082/swagger`
- **Load Testing**: [Load Test Guide](./load-tests/README.md) - k6 performance test scripts and results

## 🚀 Performance & Load Testing

The project includes comprehensive k6 load tests to validate system performance under various scenarios.

### Test Scenarios

| Test Script | Environment | Scenario | Purpose |
|-------------|-------------|----------|---------|
| `auth-load-test.js` | Docker/K8s | Authentication operations | Test login/register performance |
| `gym-load-test.js` | Docker/K8s | Gym CRUD operations | Test gym service endpoints |
| `full-system-test.js` | Docker/K8s | End-to-end workflows | Test complete user journeys |
| `realistic-load-test.js` | Docker/K8s | Mixed realistic traffic | Simulate real-world usage patterns |
| `country-scale-test.js` | Docker | Country-level load | Baseline performance testing |
| `country-scale-peak-test.js` | Docker | Peak traffic simulation | Stress testing without scaling |
| `country-scale-peak-test-k8s.js` | Kubernetes | Peak traffic with K8s | Test Kubernetes load balancing |

### Running Load Tests

**Prerequisites:**
```bash
# Install k6
choco install k6  # Windows
brew install k6   # macOS
```

**Docker Compose Environment:**
```bash
# Start services
cd deployment/docker
docker-compose up -d

# Run tests
cd ../../load-tests
k6 run country-scale-test.js
k6 run country-scale-peak-test.js
```

**Kubernetes Environment:**
```bash
# Deploy to Kubernetes
cd deployment/kubernetes
.\deploy-k8s.ps1

# Run tests (update URLs to NodePort)
cd ../../load-tests
k6 run country-scale-peak-test-k8s.js
```

### Test Results & Analysis

Load test results are documented in `load-tests/results/` with detailed analysis:

- **Docker Compose Results**: Baseline performance metrics
  - Single-instance deployment
  - Resource constraints of local development
  - Establishes performance baseline

- **Kubernetes Results**: Production-like performance
  - Multiple pod replicas with load balancing
  - Service discovery and routing overhead
  - Scalability validation

- **Future: Kubernetes with HPA**: Auto-scaling performance
  - Horizontal Pod Autoscaler (HPA) configuration
  - Dynamic scaling based on CPU/memory metrics
  - Minikube autoscaling demonstration
  - Cost vs. performance trade-offs

**Key Metrics Tracked:**
- Request rate (requests/second)
- Response time (p50, p95, p99 percentiles)
- Error rate (%)
- Concurrent users (VUs)
- System resource usage (CPU, memory)
- Database connection pool utilization

See [load-tests/README.md](./load-tests/README.md) for detailed test results and performance analysis.

## ✨ Key Features

### 🏋️ Gym Management
- Browse and search gyms
- View detailed gym information (facilities, hours, contact)
- Admin CRUD operations for gym management

### 👥 User Management
- JWT-based authentication with secure password hashing
- Role-based authorization (User, Moderator, Admin)
- User profile management with password changes
- Admin user management (role changes, activation/deactivation)

### 🎫 Membership System
- Purchase gym memberships with flexible durations
- View and manage active memberships
- Cancel memberships
- Moderator membership management for their groups

### 👥 Group Management
- Create gym-specific groups
- Assign moderators to groups
- Moderators can manage group members
- Track member counts and activity

### 🔒 Security
- JWT token-based authentication
- Password pepper and salt hashing
- Role-based route guards
- API Gateway with token validation
- Secure HTTP-only cookies

### 🎨 User Experience
- Responsive design for all devices
- Real-time form validation
- Toast notifications for user feedback
- Loading states for async operations
- Error handling with user-friendly messages

## 🤝 Contributing

1. **Fork the repository** and clone to your local machine
2. **Create a feature branch** from `dev`:
   ```bash
   git checkout -b feature/your-feature-name dev
   ```
3. **Make your changes** following the project structure
4. **Test your changes** locally:
   - Ensure all services start correctly
   - Test affected API endpoints
   - Check frontend UI changes
5. **Ensure CI pipelines pass**:
   - Run `npm run check` for frontend
   - Run `dotnet test` for backend services
6. **Commit with clear messages**:
   ```bash
   git commit -m "feat: add new feature description"
   ```
7. **Submit a pull request** to `dev` branch

### Development Guidelines
- Follow Clean Architecture principles for backend
- Use TypeScript for all frontend code
- Write unit tests for new features
- Update README if adding new features
- Keep commits atomic and focused

## 📄 License

MIT License - See [LICENSE](./LICENSE) file for details.

---