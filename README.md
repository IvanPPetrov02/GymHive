# GymHive

[![Frontend CI](https://github.com/IvanPPetrov02/GymHive/actions/workflows/FrontEndCI.yml/badge.svg?branch=dev)](https://github.com/IvanPPetrov02/GymHive/actions/workflows/FrontEndCI.yml)
[![Authentication Service CI](https://github.com/IvanPPetrov02/GymHive/actions/workflows/AuthenticationServiceCI.yml/badge.svg?branch=dev)](https://github.com/IvanPPetrov02/GymHive/actions/workflows/AuthenticationServiceCI.yml)

Cloud-native social platform for gym enthusiasts to share workouts, meals, and lifestyle tips.

## 🚀 Quick Start

### Using Docker (Recommended)

```bash
# Start all services
docker-compose up -d

# Access the application
# Frontend:  http://localhost:3000
# API:       http://localhost:8080
# Swagger:   http://localhost:8080/swagger
# Database:  localhost:3307
```

See [DOCKER.md](./DOCKER.md) for detailed Docker setup and management.

### Local Development

```bash
# Start database only
docker-compose -f docker-compose.dev.yml up -d

# Run backend
cd GymHiveBackend/AuthenticationService
dotnet run

# Run frontend (in another terminal)
cd GymHiveFrontend
npm install
npm run dev
```

## 🏗️ Architecture

This project follows a microservices architecture:

```
Frontend (Svelte) → Authentication Service (.NET) → MySQL Database
```

- **Frontend**: Svelte + TypeScript + Vite + TailwindCSS
- **Authentication Service**: ASP.NET Core 9.0 + JWT + Entity Framework Core
- **Database**: MySQL 8.0
- *(More microservices coming soon)*

## 🚀 CI/CD Pipelines

Each component has its own CI pipeline that only runs when relevant files are changed:

| Component | Status | Triggers On |
|-----------|--------|-------------|
| Frontend | [![Frontend CI](https://github.com/IvanPPetrov02/GymHive/actions/workflows/FrontEndCI.yml/badge.svg?branch=dev)](https://github.com/IvanPPetrov02/GymHive/actions/workflows/FrontEndCI.yml) | Changes in `GymHiveFrontend/**` |
| Authentication Service | [![Auth CI](https://github.com/IvanPPetrov02/GymHive/actions/workflows/AuthenticationServiceCI.yml/badge.svg?branch=dev)](https://github.com/IvanPPetrov02/GymHive/actions/workflows/AuthenticationServiceCI.yml) | Changes in `GymHiveBackend/AuthenticationService/**` |

## 📦 Project Structure

```
GymHive/
├── GymHiveFrontend/          # Svelte frontend application
├── GymHiveBackend/
│   └── AuthenticationService/ # JWT authentication microservice
├── .github/workflows/         # CI/CD pipelines
├── docker-compose.yml         # Production Docker setup
├── docker-compose.dev.yml     # Development Docker setup
└── DOCKER.md                  # Docker documentation
```

## 🛠️ Technologies

### Frontend
- **Framework**: Svelte 5 + TypeScript
- **Build Tool**: Vite
- **Styling**: TailwindCSS
- **Routing**: svelte-spa-router
- **State Management**: Svelte stores

### Backend
- **Framework**: ASP.NET Core 9.0
- **Database**: MySQL 8.0 with Entity Framework Core
- **Authentication**: JWT Bearer tokens
- **API Documentation**: Swagger/OpenAPI
- **Architecture**: Clean Architecture (BLL/DAL separation)

### DevOps
- **Containerization**: Docker + Docker Compose
- **CI/CD**: GitHub Actions (separate pipelines per service)
- **Version Control**: Git + GitHub

## 📚 Documentation

- [DOCKER.md](./DOCKER.md) - Complete Docker setup and management guide
- [.github/workflows/](./github/workflows/) - CI/CD pipeline configurations

## 🤝 Contributing

1. Create a feature branch from `dev`
2. Make your changes
3. Ensure CI pipelines pass
4. Submit a pull request to `dev`

## 📄 License

See [LICENSE](./LICENSE) file for details.

