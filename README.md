# GymHive

[![Frontend CI](https://github.com/IvanPPetrov02/GymHive/actions/workflows/FrontEndCI.yml/badge.svg?branch=dev)](https://github.com/IvanPPetrov02/GymHive/actions/workflows/FrontEndCI.yml)
[![Authentication Service CI](https://github.com/IvanPPetrov02/GymHive/actions/workflows/AuthenticationServiceCI.yml/badge.svg?branch=dev)](https://github.com/IvanPPetrov02/GymHive/actions/workflows/AuthenticationServiceCI.yml)
[![Master CI](https://github.com/IvanPPetrov02/GymHive/actions/workflows/MasterCI.yml/badge.svg?branch=dev)](https://github.com/IvanPPetrov02/GymHive/actions/workflows/MasterCI.yml)

Cloud-native social platform for gym enthusiasts to share workouts, meals, and lifestyle tips.

## 🏗️ Architecture

This project follows a microservices architecture with separate CI/CD pipelines for each component:

- **Frontend**: Svelte + TypeScript + Vite
- **Authentication Service**: ASP.NET Core 9.0 + JWT + MySQL
- *(More microservices coming soon)*

## 🚀 CI/CD Pipelines

Each component has its own CI pipeline that only runs when relevant files are changed:

| Component | Status | Triggers On |
|-----------|--------|-------------|
| Frontend | [![Frontend CI](https://github.com/IvanPPetrov02/GymHive/actions/workflows/FrontEndCI.yml/badge.svg?branch=dev)](https://github.com/IvanPPetrov02/GymHive/actions/workflows/FrontEndCI.yml) | Changes in `GymHiveFrontend/**` |
| Authentication Service | [![Auth CI](https://github.com/IvanPPetrov02/GymHive/actions/workflows/AuthenticationServiceCI.yml/badge.svg?branch=dev)](https://github.com/IvanPPetrov02/GymHive/actions/workflows/AuthenticationServiceCI.yml) | Changes in `GymHiveBackend/AuthenticationService/**` |

