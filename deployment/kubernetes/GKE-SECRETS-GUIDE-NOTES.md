GitHub Actions: repository secrets and workflow

Add the following repository secrets to enable the `.github/workflows/sync-secret-manager.yml` workflow to populate Secret Manager and grant access to ExternalSecrets:

- `GCP_SA_KEY`: JSON service account key with permissions to create/modify Secret Manager secrets and to add IAM policy bindings for the ExternalSecrets GSA (best to scope this SA to Secret Manager only).
- `GCP_PROJECT_ID`: your GCP project id (for example: `gymhive-481310`).
- `MYSQL_ROOT_PASSWORD`: the MySQL root password used by in-cluster MySQL deployments (the workflow can use this to construct DB connection strings).
- `EXTERNAL_SECRETS_GSA` (optional): the email of the GCP Service Account the ExternalSecrets operator uses (defaults to the value annotated on the `external-secrets` KSA).

Recommendations:

- Prefer Workload Identity Federation (`google-github-actions/auth`) instead of a long-lived SA key stored in GitHub secrets. This removes the need to keep a JSON key in the repo secrets and is more secure.
- Limit the SA permissions to Secret Manager (and only the secrets used by GymHive) and to the minimal IAM binding operations required.
- Use environment-specific secrets (e.g., `MYSQL_ROOT_PASSWORD_PROD`) for production vs. staging.

How the workflow is used:

- The workflow `./github/workflows/sync-secret-manager.yml` runs on `push` to `main` and will create or add versions to Secret Manager secrets, grant `roles/secretmanager.secretAccessor` to the ExternalSecrets GSA, and annotate ExternalSecrets to force a refresh in-cluster.
- Ensure the repository secrets above are configured before merging to `main`, or the workflow will fail due to missing credentials.

Notes:

- If you prefer not to store an SA key in GitHub, configure Workload Identity Federation and update the workflow to use `google-github-actions/auth` with OIDC.
- After adding the secrets, trigger a push to `main` or run the workflow manually to populate Secret Manager.
