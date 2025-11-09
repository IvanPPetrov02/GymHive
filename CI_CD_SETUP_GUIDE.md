# CI/CD Pipeline Setup Guide

## ✅ What's Been Fixed

Your CI pipelines now properly **build AND push** Docker images to Docker Hub.

### Updated CI Workflows:
- ✅ **AuthenticationService** - Builds, tests, and pushes Docker image
- ✅ **GymService** - Builds, tests, and pushes Docker image
- ✅ **MembershipService** - Builds, tests, and pushes Docker image (NEW!)
- ✅ **ApiGateway** - Builds, tests, and pushes Docker image

## 🔧 Setup Required

### Step 1: Create Docker Hub Account (if you don't have one)

1. Go to https://hub.docker.com/
2. Sign up for a free account
3. Remember your username - you'll need it

### Step 2: Create Docker Hub Access Token

1. Log into Docker Hub
2. Click on your username → **Account Settings**
3. Click **Security** → **New Access Token**
4. Token Description: `GitHub Actions CI/CD`
5. Access permissions: **Read, Write, Delete**
6. Click **Generate**
7. **COPY THE TOKEN IMMEDIATELY** (you won't see it again!)

### Step 3: Add Secrets to GitHub Repository

1. Go to your GitHub repository: `https://github.com/IvanPPetrov02/GymHive`
2. Click **Settings** → **Secrets and variables** → **Actions**
3. Click **New repository secret**
4. Add these two secrets:

**Secret 1: DOCKER_USERNAME**
- Name: `DOCKER_USERNAME`
- Value: Your Docker Hub username (e.g., `ivanppetrov`)
- Click **Add secret**

**Secret 2: DOCKER_PASSWORD**
- Name: `DOCKER_PASSWORD`
- Value: The access token you just copied
- Click **Add secret**

## 📊 How the CI/CD Pipeline Works Now

### CI (Continuous Integration) - Automatic on Push

```
Push to GitHub
   ↓
1. Checkout code
2. Setup .NET
3. Restore dependencies
4. Build solution
5. Run tests
6. Publish application
7. Build Docker image
8. Push to Docker Hub ← NEW!
   ↓
Docker image available: 
yourusername/gymhive-auth-service:latest
yourusername/gymhive-gym-service:latest
yourusername/gymhive-membership-service:latest
yourusername/gymhive-api-gateway:latest
```

### Image Tags

Each push creates multiple tags:
- `latest` - Only on main branch
- `dev` - On dev branch
- `dev-abc123` - Branch name + commit SHA

Example:
```
ivanppetrov/gymhive-auth-service:latest
ivanppetrov/gymhive-auth-service:dev
ivanppetrov/gymhive-auth-service:dev-abc1234
```

## 🚀 CD (Continuous Deployment) - What's Next

### Option 1: Manual Deployment (Simple)

Update `docker-compose.yml` to use your Docker Hub images:

```yaml
services:
  auth-service:
    image: yourusername/gymhive-auth-service:latest
    # Remove build section
    
  gym-service:
    image: yourusername/gymhive-gym-service:latest
    # Remove build section
    
  membership-service:
    image: yourusername/gymhive-membership-service:latest
    # Remove build section
    
  api-gateway:
    image: yourusername/gymhive-api-gateway:latest
    # Remove build section
```

Then deploy:
```bash
docker-compose pull  # Pull latest images
docker-compose up -d # Start services
```

### Option 2: GitHub Actions CD (Automated)

Create `.github/workflows/deploy.yml`:

```yaml
name: Deploy to Production

on:
  push:
    branches: [ main ]
  workflow_dispatch:

jobs:
  deploy:
    name: Deploy to Server
    runs-on: ubuntu-latest
    
    steps:
      - name: Deploy to Server via SSH
        uses: appleboy/ssh-action@v1.0.0
        with:
          host: ${{ secrets.SERVER_HOST }}
          username: ${{ secrets.SERVER_USER }}
          key: ${{ secrets.SERVER_SSH_KEY }}
          script: |
            cd /path/to/gymhive
            docker-compose pull
            docker-compose up -d
            docker system prune -f
```

### Option 3: Watchtower (Auto-Update)

Add Watchtower to `docker-compose.yml` for automatic updates:

```yaml
  watchtower:
    image: containrrr/watchtower
    volumes:
      - /var/run/docker.sock:/var/run/docker.sock
    command: --interval 300 --cleanup
    # Checks for new images every 5 minutes
```

## 🔍 Verify Setup

### Test the CI Pipeline

1. Make a small change to any service
2. Commit and push to `dev` branch:
   ```bash
   git add .
   git commit -m "Test CI pipeline"
   git push origin dev
   ```

3. Go to GitHub → **Actions** tab
4. Watch the workflow run
5. Check Docker Hub for the new image:
   - Visit: `https://hub.docker.com/r/yourusername/gymhive-auth-service`
   - You should see the new tag

### Test Pulling the Image

```bash
docker pull yourusername/gymhive-auth-service:dev
docker images | grep gymhive
```

## 📋 Complete CI/CD Flow

### Development Workflow:

```
1. Developer pushes code to dev branch
   ↓
2. GitHub Actions CI runs:
   - Builds code
   - Runs tests
   - Builds Docker image
   - Pushes to Docker Hub as dev tag
   ↓
3. Dev server (manual or auto):
   - Pulls dev image
   - Restarts containers
   ↓
4. Testing on dev environment
```

### Production Workflow:

```
1. Merge dev → main (via Pull Request)
   ↓
2. GitHub Actions CI runs:
   - Builds code
   - Runs tests
   - Builds Docker image
   - Pushes to Docker Hub as latest tag
   ↓
3. Production server (manual or auto):
   - Pulls latest image
   - Restarts containers with zero downtime
   ↓
4. Production deployment complete
```

## 🎯 Benefits of This Setup

✅ **Centralized Images**
- All images stored in Docker Hub
- No need to rebuild on servers
- Consistent across environments

✅ **Version Control**
- Every commit has a tagged image
- Easy rollback to any version
- Full audit trail

✅ **Fast Deployments**
- Just pull image and restart
- No build time on servers
- Deploy to multiple servers easily

✅ **Separation of Concerns**
- CI: Build and test
- CD: Pull and deploy
- Clear pipeline stages

## 🔒 Security Best Practices

1. **Never commit Docker Hub credentials to code**
   - Use GitHub Secrets ✅
   
2. **Use access tokens, not passwords**
   - Tokens can be revoked ✅
   - Limited permissions ✅

3. **Scan images for vulnerabilities**
   - Docker Hub auto-scans
   - Add Trivy to CI (optional)

4. **Use specific tags in production**
   - Avoid `latest` in prod
   - Use SHA tags for immutability

## 🚀 Next Steps

1. ✅ Add Docker Hub secrets to GitHub
2. ✅ Push code to trigger CI
3. ✅ Verify images appear in Docker Hub
4. ✅ Update docker-compose.yml to use images
5. ✅ Test deployment by pulling and running images
6. ⏳ (Optional) Set up automated CD with GitHub Actions
7. ⏳ (Optional) Add Watchtower for auto-updates

## 📚 Resources

- Docker Hub: https://hub.docker.com
- GitHub Actions: https://github.com/features/actions
- Docker Compose: https://docs.docker.com/compose
- Watchtower: https://containrrr.github.io/watchtower

---

**Your CI pipeline is now complete!** 🎉

The images will be built and pushed to Docker Hub automatically on every push to `main` or `dev` branches.
