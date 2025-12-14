# Managing Secrets in Google Kubernetes Engine (GKE)

## Option 1: Using Google Secret Manager (Recommended for Production)

### Step 1: Enable Secret Manager API
```bash
gcloud services enable secretmanager.googleapis.com
```

### Step 2: Create Secrets in Google Secret Manager
```bash
# From your .env file, create each secret
gcloud secrets create jwt-secret --data-file=- <<EOF
YourSuperSecretKeyThatIsAtLeast32CharactersLong!
EOF

gcloud secrets create password-pepper --data-file=- <<EOF
GymPepper
EOF

gcloud secrets create rabbitmq-password --data-file=- <<EOF
GymHive123!
EOF

gcloud secrets create mysql-root-password --data-file=- <<EOF
RootPassword123!
EOF

gcloud secrets create mysql-password --data-file=- <<EOF
GymHive123!
EOF

gcloud secrets create mongodb-root-password --data-file=- <<EOF
RootPassword123!
EOF
```

### Step 3: Grant GKE Service Account Access
```bash
# Get your project ID
PROJECT_ID=$(gcloud config get-value project)

# Get your GKE cluster's service account
GKE_SA=$(gcloud iam service-accounts list --filter="email:*compute@developer.gserviceaccount.com" --format="value(email)")

# Grant access to each secret
for secret in jwt-secret password-pepper rabbitmq-password mysql-root-password mysql-password mongodb-root-password; do
  gcloud secrets add-iam-policy-binding $secret \
    --member="serviceAccount:${GKE_SA}" \
    --role="roles/secretmanager.secretAccessor"
done
```

### Step 4: Install Secret Store CSI Driver in GKE
```bash
# Enable Workload Identity
gcloud container clusters update YOUR-CLUSTER-NAME \
  --workload-pool=${PROJECT_ID}.svc.id.goog

# Install the Secret Store CSI driver addon
gcloud container clusters update YOUR-CLUSTER-NAME \
  --update-addons=GcpSecretManagerCsiDriver=ENABLED
```

### Step 5: Update Kubernetes Deployments to Use Secret Manager

Create a SecretProviderClass:
```yaml
apiVersion: secrets-store.csi.x-k8s.io/v1
kind: SecretProviderClass
metadata:
  name: gymhive-secrets
  namespace: gymhive
spec:
  provider: gcp
  parameters:
    secrets: |
      - resourceName: "projects/${PROJECT_ID}/secrets/jwt-secret/versions/latest"
        path: "jwt-secret"
      - resourceName: "projects/${PROJECT_ID}/secrets/password-pepper/versions/latest"
        path: "password-pepper"
      - resourceName: "projects/${PROJECT_ID}/secrets/rabbitmq-password/versions/latest"
        path: "rabbitmq-password"
      - resourceName: "projects/${PROJECT_ID}/secrets/mysql-root-password/versions/latest"
        path: "mysql-root-password"
      - resourceName: "projects/${PROJECT_ID}/secrets/mysql-password/versions/latest"
        path: "mysql-password"
      - resourceName: "projects/${PROJECT_ID}/secrets/mongodb-root-password/versions/latest"
        path: "mongodb-root-password"
  secretObjects:
  - secretName: gymhive-secrets
    type: Opaque
    data:
    - objectName: "jwt-secret"
      key: "JWT_SECRET"
    - objectName: "password-pepper"
      key: "PASSWORD_PEPPER"
    - objectName: "rabbitmq-password"
      key: "RABBITMQ_PASSWORD"
    - objectName: "mysql-root-password"
      key: "MYSQL_ROOT_PASSWORD"
    - objectName: "mysql-password"
      key: "MYSQL_PASSWORD"
    - objectName: "mongodb-root-password"
      key: "MONGODB_ROOT_PASSWORD"
```

Update your deployments to mount the secrets:
```yaml
volumeMounts:
- name: secrets-store
  mountPath: "/mnt/secrets-store"
  readOnly: true

volumes:
- name: secrets-store
  csi:
    driver: secrets-store.csi.k8s.io
    readOnly: true
    volumeAttributes:
      secretProviderClass: "gymhive-secrets"
```

Then reference in env:
```yaml
env:
- name: JWT__Secret
  valueFrom:
    secretKeyRef:
      name: gymhive-secrets
      key: JWT_SECRET
```

## Option 2: Using Kubernetes Secrets in GKE (Easier, Less Secure)

### Step 1: Create Kubernetes Secret from .env file
```bash
# Navigate to your project directory
cd "c:\Users\vanit\Desktop\CSS - Semester 6\individual project\GymHive"

# Create secret from .env file (already done locally)
kubectl create secret generic gymhive-secrets \
  --from-env-file=.env \
  -n gymhive \
  --dry-run=client -o yaml > deployment/kubernetes/secrets-gke.yaml

# Apply to GKE cluster
kubectl apply -f deployment/kubernetes/secrets-gke.yaml
```

### Step 2: Enable Encryption at Rest in GKE
```bash
gcloud container clusters update YOUR-CLUSTER-NAME \
  --database-encryption-key projects/${PROJECT_ID}/locations/REGION/keyRings/KEYRING_NAME/cryptoKeys/KEY_NAME
```

## Option 3: Using Google Cloud KMS (Best Security)

### Step 1: Create KMS Keyring and Key
```bash
# Create keyring
gcloud kms keyrings create gymhive-keyring --location=global

# Create encryption key
gcloud kms keys create gymhive-secrets-key \
  --location=global \
  --keyring=gymhive-keyring \
  --purpose=encryption
```

### Step 2: Encrypt Secrets
```bash
# Encrypt each secret
echo -n "YourSuperSecretKeyThatIsAtLeast32CharactersLong!" | \
  gcloud kms encrypt \
  --location=global \
  --keyring=gymhive-keyring \
  --key=gymhive-secrets-key \
  --plaintext-file=- \
  --ciphertext-file=jwt-secret.enc

# Store encrypted secrets in Git safely
```

### Step 3: Decrypt at Runtime
Use init containers or startup scripts to decrypt secrets before the main container starts.

## Comparison of Options

| Feature | Secret Manager | K8s Secrets + KMS | K8s Secrets Only |
|---------|----------------|-------------------|------------------|
| Security | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐⭐ |
| Ease of Use | ⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ |
| Audit Logging | ✅ Full | ✅ Partial | ❌ Limited |
| Secret Rotation | ✅ Automatic | ⚠️ Manual | ⚠️ Manual |
| Cross-service | ✅ Yes | ❌ No | ❌ No |
| Cost | $ per secret | $ per key | Free |

## Recommended Approach for GymHive

**For Development/Testing (Current):**
- Use Kubernetes Secrets (Option 2) ✅ Already implemented

**For Production:**
- Use Google Secret Manager (Option 1) with Secret Store CSI Driver
- Enable GKE encryption at rest
- Set up secret rotation policies
- Use Workload Identity for service accounts

## Quick Setup for GKE Production

```bash
# 1. Set variables
export PROJECT_ID=$(gcloud config get-value project)
export CLUSTER_NAME="gymhive-cluster"
export REGION="us-central1"

# 2. Create cluster with Workload Identity
gcloud container clusters create ${CLUSTER_NAME} \
  --region=${REGION} \
  --num-nodes=3 \
  --enable-autoscaling \
  --min-nodes=3 \
  --max-nodes=10 \
  --workload-pool=${PROJECT_ID}.svc.id.goog \
  --addons=GcpSecretManagerCsiDriver

# 3. Create secrets in Secret Manager (see Step 2 above)

# 4. Deploy application
kubectl apply -f deployment/kubernetes/namespace.yaml
kubectl apply -f deployment/kubernetes/secrets.yaml  # For non-sensitive configs
kubectl apply -f deployment/kubernetes/secret-provider-class.yaml  # For Secret Manager
kubectl apply -f deployment/kubernetes/databases/
kubectl apply -f deployment/kubernetes/services/

# 5. Verify secrets are mounted
kubectl exec -it deployment/auth-service -n gymhive -- ls /mnt/secrets-store
```

## Security Best Practices

1. **Never commit .env files to Git** - Add to .gitignore
2. **Use different secrets for each environment** (dev/staging/prod)
3. **Rotate secrets regularly** (every 90 days minimum)
4. **Use least privilege IAM** - Grant only necessary permissions
5. **Enable audit logging** - Monitor secret access
6. **Encrypt secrets at rest** - Enable GKE encryption
7. **Use Workload Identity** - Don't use service account keys

## Current Implementation Status

✅ Local development: Using Kubernetes Secrets from .env file
✅ All services configured to use secrets via ConfigMap/Secret references
✅ Secrets separated from application code
⏳ GKE: Ready to migrate to Secret Manager when deploying to production

## Migration Checklist for GKE

- [ ] Enable Secret Manager API
- [ ] Create secrets in Secret Manager
- [ ] Configure Workload Identity
- [ ] Install Secret Store CSI Driver
- [ ] Create SecretProviderClass
- [ ] Update deployments to mount secrets
- [ ] Test secret access
- [ ] Remove old Kubernetes secrets
- [ ] Set up secret rotation
- [ ] Configure monitoring/alerts
