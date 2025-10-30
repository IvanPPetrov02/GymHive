# GymHive

[![Frontend CI](https://github.com/IvanPPetrov02/GymHive/actions/workflows/FrontEndCI.yml/badge.svg?branch=dev)](https://github.com/IvanPPetrov02/GymHive/actions/workflows/FrontEndCI.yml)
[![Authentication Service CI](https://github.com/IvanPPetrov02/GymHive/actions/workflows/AuthenticationServiceCI.yml/badge.svg?branch=dev)](https://github.com/IvanPPetrov02/GymHive/actions/workflows/AuthenticationServiceCI.yml)
[![Gym Service CI](https://github.com/IvanPPetrov02/GymHive/actions/workflows/GymServiceCI.yml/badge.svg?branch=dev)](https://github.com/IvanPPetrov02/GymHive/actions/workflows/GymServiceCI.yml)
[![API Gateway CI](https://github.com/IvanPPetrov02/GymHive/actions/workflows/ApiGatewayCI.yml/badge.svg?branch=dev)](https://github.com/IvanPPetrov02/GymHive/actions/workflows/ApiGatewayCI.yml)

Cloud-native microservices platform for gym management with membership systems, group management, and role-based administration.

## 🚀 Quick Start

### Using Docker (Recommended)

```bash
# Start all services
docker-compose up -d

# Access the application
# Frontend:         http://localhost:3000
# API Gateway:      http://localhost:5000
# Auth Service:     http://localhost:8080
# Gym Service:      http://localhost:8081
# Auth Database:    localhost:3307
# Gym Database:     localhost:3308
```

See [DOCKER.md](./DOCKER.md) for detailed Docker setup and management.

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

## 🏗️ Architecture

This project follows a **microservices architecture** with an API Gateway pattern:

```
┌─────────────────┐
│   Frontend      │
│  (Svelte SPA)   │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  API Gateway    │
│   (YARP Proxy)  │
└────┬────────┬───┘
     │        │
     ▼        ▼
┌─────────┐ ┌──────────┐
│  Auth   │ │   Gym    │
│ Service │ │ Service  │
└────┬────┘ └────┬─────┘
     │           │
     ▼           ▼
┌─────────┐ ┌──────────┐
│Auth DB  │ │ Gym DB   │
│(MySQL)  │ │ (MySQL)  │
└─────────┘ └──────────┘
```

### Services
- **Frontend**: Svelte 5 + TypeScript + Vite + TailwindCSS
- **API Gateway**: ASP.NET Core YARP reverse proxy with authentication
- **Authentication Service**: JWT-based user authentication and management
- **Gym Service**: Gyms, gym groups, and membership management
- **Databases**: Separate MySQL 8.0 databases per service (database-per-service pattern)

## 🚀 CI/CD Pipelines

Each component has its own CI pipeline that only runs when relevant files are changed:

| Component | Status | Triggers On |
|-----------|--------|-------------|
| Frontend | [![Frontend CI](https://github.com/IvanPPetrov02/GymHive/actions/workflows/FrontEndCI.yml/badge.svg?branch=dev)](https://github.com/IvanPPetrov02/GymHive/actions/workflows/FrontEndCI.yml) | Changes in `GymHiveFrontend/**` |
| Authentication Service | [![Auth CI](https://github.com/IvanPPetrov02/GymHive/actions/workflows/AuthenticationServiceCI.yml/badge.svg?branch=dev)](https://github.com/IvanPPetrov02/GymHive/actions/workflows/AuthenticationServiceCI.yml) | Changes in `GymHiveBackend/AuthenticationService/**` |
| Gym Service | [![Gym CI](https://github.com/IvanPPetrov02/GymHive/actions/workflows/GymServiceCI.yml/badge.svg?branch=dev)](https://github.com/IvanPPetrov02/GymHive/actions/workflows/GymServiceCI.yml) | Changes in `GymHiveBackend/GymService/**` |
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
│   └── GymService/               # Gym Management Microservice
│       ├── Controllers/
│       │   ├── GymsController.cs
│       │   ├── GymGroupsController.cs
│       │   └── MembershipsController.cs
│       ├── BLL/                  # Business Logic Layer
│       │   ├── Managers/
│       │   ├── DTOs/
│       │   └── Entities/         # Gym, GymGroup, Membership entities
│       ├── DAL/                  # Data Access Layer
│       ├── Tests/
│       └── Dockerfile
│
├── .github/workflows/            # CI/CD Pipelines
│   ├── FrontEndCI.yml
│   ├── AuthenticationServiceCI.yml
│   ├── GymServiceCI.yml
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
- **CI/CD**: GitHub Actions (separate pipelines per service)
- **Version Control**: Git + GitHub
- **Orchestration**: Docker Compose with health checks

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

## �📚 Documentation

- [DOCKER.md](./DOCKER.md) - Complete Docker setup and management guide
- [.github/workflows/](./.github/workflows/) - CI/CD pipeline configurations
- **Swagger UI**: 
  - Auth Service: `http://localhost:8080/swagger`
  - Gym Service: `http://localhost:8081/swagger`

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