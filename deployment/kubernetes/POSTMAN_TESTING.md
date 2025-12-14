# GymHive API Testing with Postman

## Current Status
The API Gateway is running but using **old code** that doesn't include GetUser in the public paths.

## Port Forwarding
- **Frontend**: http://localhost:3000
- **API Gateway**: http://localhost:5000

---

## Postman Test Collection

### 1. Health Check (No Auth Required)
```
GET http://localhost:5000/health
```
**Expected**: 200 OK with health status JSON

---

### 2. Register User (No Auth Required)
```
POST http://localhost:5000/api/auth/register
Content-Type: application/json

{
  "email": "test@example.com",
  "password": "Test123!",
  "name": "Test",
  "surname": "User"
}
```
**Expected**: 200 OK with "User created" message

---

### 3. Login (No Auth Required)
```
POST http://localhost:5000/api/auth/login
Content-Type: application/json

{
  "email": "test@example.com",
  "password": "Test123!"
}
```
**Expected**: 200 OK with JWT token
**Response**:
```json
{
  "token": "eyJhbGc...",
  "expiresIn": 3600
}
```
**Save the token** for the next requests!

---

### 4. Get User Profile (Currently Returns 401 - BUG!)
```
GET http://localhost:5000/api/auth/GetUser
Authorization: Bearer <your-token-here>
```
**Current Behavior**: 401 Unauthorized (old code blocking it)
**Expected After Fix**: 200 OK with user profile

---

## The Problem

The API Gateway pod is running **old code**. The updated code (with GetUser in public paths) is in your local repository but NOT in the Docker image that Kubernetes is using.

## Solutions

### Option 1: Build Locally in Minikube (Recommended for Testing)
```powershell
# Use minikube's Docker daemon
& minikube -p minikube docker-env --shell powershell | Invoke-Expression

# Build the image inside minikube
cd GymHiveBackend/ApiGateway
docker build -t ivanppetrov/gymhive-api-gateway:latest .

# Set imagePullPolicy to Never in api-gateway.yaml
# Then restart the pod
kubectl delete pod -n gymhive -l app=api-gateway
```

### Option 2: Push to Docker Hub (For Production)
```bash
cd GymHiveBackend/ApiGateway
docker build -t ivanppetrov/gymhive-api-gateway:latest .
docker push ivanppetrov/gymhive-api-gateway:latest

# Then restart pod
kubectl delete pod -n gymhive -l app=api-gateway
```

---

## Testing Flow with Postman

1. **Health Check** → Verify API is accessible
2. **Register** → Create a new user
3. **Login** → Get JWT token
4. **Get User Profile** → Test with the token (currently fails with 401)

After fixing the Docker image, step 4 should return user profile successfully!
