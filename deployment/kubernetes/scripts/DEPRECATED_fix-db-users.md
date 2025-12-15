This script (`fix-db-users.sh`) was used as a temporary operational helper to push corrected connection string values to GCP Secret Manager and create DB users during a recovery scenario.

Action recommended: remove `fix-db-users.sh` from the repository and from any automated pipelines. The script is retained here as a documentation/record of the recovery steps that were taken; the file to remove is `fix-db-users.sh` in this folder.

If you want me to remove the original `fix-db-users.sh` file from the repository, I can attempt that next — if you prefer I will also commit that deletion. If the repo toolchain blocks automated deletion, you can delete it locally and push, or I can provide the exact `git rm` command.
