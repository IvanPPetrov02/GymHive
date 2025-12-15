#!/usr/bin/env bash
set -euo pipefail

# Fix DB connection secrets and create DB users for GymHive (auth & notifications)
# Usage: run from Cloud Shell where `kubectl` and `gcloud` are configured.

NAMESPACE=${NAMESPACE:-gymhive}
PROJECT_ID=$(gcloud config get-value project 2>/dev/null || true)
if [ -z "$PROJECT_ID" ]; then
  echo "gcloud project not set. Run: gcloud config set project <PROJECT_ID>" >&2
  exit 1
fi

echo "Using project: $PROJECT_ID  namespace: $NAMESPACE"

kc_get_conn() {
  kubectl -n "$NAMESPACE" get secret "$1" -o jsonpath="{.data.CONNECTION_STRING}" 2>/dev/null || true
}

decode() { echo "$1" | base64 --decode 2>/dev/null || echo ""; }

split_servers() {
  # split a connection-string blob into segments starting with 'Server='
  # prints each segment on its own line
  awk -v RS="Server=" 'NR>1{print "Server="$0}' <<<"$1"
}

extract_field() {
  # args: <conn-string> <KeyName>
  # returns value for KeyName (e.g., Uid, Pwd, Database)
  sed -n "s/.*$2=\([^;]*\).*/\1/p" <<<"$1" || true
}

echo "Reading current Kubernetes secrets..."
auth_enc=$(kc_get_conn gymhive-auth-connection)
notif_enc=$(kc_get_conn gymhive-notifications-connection)
gym_enc=$(kc_get_conn gymhive-gym-connection)
workout_enc=$(kc_get_conn gymhive-workout-connection)

auth_conn=$(decode "$auth_enc")
notif_conn=$(decode "$notif_enc")

echo "auth connection (raw): $auth_conn"

# If auth_conn contains both auth and notifications, split and pick correct parts
if grep -q "Server=.*notifications-db" <<<"$auth_conn" || grep -q "Server=.*gym-db" <<<"$auth_conn"; then
  echo "Detected multiple server segments in the auth secret; attempting to split..."
  segments=$(split_servers "$auth_conn")
  auth_piece=""
  notif_piece=""
  for seg in $segments; do
    if [[ "$seg" == *"gym-db."* ]]; then
      auth_piece="$seg"
    fi
    if [[ "$seg" == *"notifications-db."* ]]; then
      notif_piece="$seg"
    fi
  done
  if [ -n "$auth_piece" ]; then
    auth_conn="$auth_piece"
    echo "Extracted auth_conn: $auth_conn"
  fi
  if [ -n "$notif_piece" ]; then
    notif_conn="$notif_piece"
    echo "Extracted notif_conn: $notif_conn"
  fi
fi

if [ -z "$auth_conn" ]; then
  echo "Warning: auth connection string empty. Aborting." >&2
  exit 1
fi

# Parse values
AUTH_UID=$(extract_field "$auth_conn" "Uid")
AUTH_PWD=$(extract_field "$auth_conn" "Pwd")
AUTH_DB=$(extract_field "$auth_conn" "Database")

NOTIF_UID=$(extract_field "$notif_conn" "Uid")
NOTIF_PWD=$(extract_field "$notif_conn" "Pwd")
NOTIF_DB=$(extract_field "$notif_conn" "Database")

echo "Parsed: auth user=$AUTH_UID db=$AUTH_DB  notif user=$NOTIF_UID db=$NOTIF_DB"

if [ -z "$AUTH_UID" ] || [ -z "$AUTH_PWD" ]; then
  echo "Auth credentials missing; aborting" >&2
  exit 1
fi

if [ -z "$NOTIF_UID" ] || [ -z "$NOTIF_PWD" ]; then
  echo "Notifications credentials missing; aborting" >&2
  exit 1
fi

update_secret_manager() {
  local secret_name="$1"
  local value="$2"
  echo "Updating Secret Manager secret: $secret_name"
  printf "%s" "$value" | gcloud secrets versions add "$secret_name" --data-file=- --project="$PROJECT_ID"
}

echo "Updating Secret Manager secrets from current Kubernetes secrets..."
update_secret_manager auth-db-connection-string "$auth_conn"
update_secret_manager notifications-db-connection-string "$notif_conn"

echo "Deleting local Kubernetes secrets to force ExternalSecrets re-create..."
kubectl -n "$NAMESPACE" delete secret gymhive-auth-connection --ignore-not-found
kubectl -n "$NAMESPACE" delete secret gymhive-notifications-connection --ignore-not-found

echo "Waiting for ExternalSecrets to re-create the Kubernetes secrets (timeout 90s)"
for i in {1..45}; do
  sleep 2
  cur=$(kubectl -n "$NAMESPACE" get secret gymhive-auth-connection -o jsonpath="{.data.CONNECTION_STRING}" 2>/dev/null || true)
  cur_dec=$(decode "$cur")
  if [ "$cur_dec" = "$auth_conn" ]; then
    echo "gymhive-auth-connection re-created"
    break
  fi
done

for i in {1..45}; do
  sleep 2
  cur=$(kubectl -n "$NAMESPACE" get secret gymhive-notifications-connection -o jsonpath="{.data.CONNECTION_STRING}" 2>/dev/null || true)
  cur_dec=$(decode "$cur")
  if [ "$cur_dec" = "$notif_conn" ]; then
    echo "gymhive-notifications-connection re-created"
    break
  fi
done

echo "Preparing and applying Kubernetes Jobs to create DB users (auth & notifications)"

AUTH_JOB_NAME=create-auth-db-user
NOTIF_JOB_NAME=create-notifications-db-user

# Ensure the gymhive-mysql-password secret exists and has the expected key
if ! kubectl -n "$NAMESPACE" get secret gymhive-mysql-password >/dev/null 2>&1; then
  echo "Required secret gymhive-mysql-password not found in namespace $NAMESPACE" >&2
  echo "Ensure ExternalSecrets created it or create a GCP Secret Manager secret named mysql-root-password with the root password and grant access." >&2
  exit 1
fi

cat <<'EOF' | kubectl -n "$NAMESPACE" apply -f -
apiVersion: batch/v1
kind: Job
metadata:
  name: $AUTH_JOB_NAME
spec:
  backoffLimit: 0
  template:
    spec:
      restartPolicy: Never
      containers:
        - name: create-auth-user
          image: mysql:8.0
          command:
            - sh
            - -c
            - |
              mysql -h gym-db.gymhive.svc.cluster.local -u root -p"\$MYSQL_ROOT_PASSWORD" -e "CREATE USER IF NOT EXISTS '${AUTH_UID}'@'%' IDENTIFIED BY '${AUTH_PWD}'; GRANT ALL PRIVILEGES ON ${AUTH_DB}.* TO '${AUTH_UID}'@'%'; FLUSH PRIVILEGES;"
          env:
            - name: MYSQL_ROOT_PASSWORD
              valueFrom:
                secretKeyRef:
                  name: gymhive-mysql-password
                  key: MYSQL_ROOT_PASSWORD
      serviceAccountName: default

---
apiVersion: batch/v1
kind: Job
metadata:
  name: $NOTIF_JOB_NAME
spec:
  backoffLimit: 0
  template:
    spec:
      restartPolicy: Never
      containers:
        - name: create-notif-user
          image: mysql:8.0
          command:
            - sh
            - -c
            - |
              mysql -h notifications-db.gymhive.svc.cluster.local -u root -p"\$MYSQL_ROOT_PASSWORD" -e "CREATE USER IF NOT EXISTS '${NOTIF_UID}'@'%' IDENTIFIED BY '${NOTIF_PWD}'; GRANT ALL PRIVILEGES ON ${NOTIF_DB}.* TO '${NOTIF_UID}'@'%'; FLUSH PRIVILEGES;"
          env:
            - name: MYSQL_ROOT_PASSWORD
              valueFrom:
                secretKeyRef:
                  name: gymhive-mysql-password
                  key: MYSQL_ROOT_PASSWORD
      serviceAccountName: default
EOF

echo "Waiting for jobs to complete (timeout 60s each)"
kubectl -n "$NAMESPACE" wait --for=condition=complete job/$AUTH_JOB_NAME --timeout=60s || true
kubectl -n "$NAMESPACE" logs -l job-name=$AUTH_JOB_NAME --tail=200 || true

kubectl -n "$NAMESPACE" wait --for=condition=complete job/$NOTIF_JOB_NAME --timeout=60s || true
kubectl -n "$NAMESPACE" logs -l job-name=$NOTIF_JOB_NAME --tail=200 || true

echo "Restarting deployments to pick up secrets"
kubectl -n "$NAMESPACE" rollout restart deployment/auth-service || true
kubectl -n "$NAMESPACE" rollout restart deployment/notifications-service || true

echo "Done. Check pods: kubectl -n $NAMESPACE get pods"
