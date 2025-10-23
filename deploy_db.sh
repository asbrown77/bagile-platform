#!/bin/bash
set -e

# ==========================================================
#  Deploy database migrations via Flyway
#  Usage: bash deploy_db.sh [env]
#  Example: bash deploy_db.sh prod
# ==========================================================

ENV=${1:-dev}
REPO_DIR=/opt/bagile
MIGRATIONS_DIR="${REPO_DIR}/migrations"
ENV_FILE="${REPO_DIR}/config/bagile.${ENV}.env"

# --- Check environment file exists ---
if [ ! -f "$ENV_FILE" ]; then
  echo "❌ Environment file not found: $ENV_FILE"
  exit 1
fi

# --- Load environment variables (POSTGRES_DB, USER, PASS, etc.) ---
source "$ENV_FILE"

echo "🚀 Deploying migrations to ${POSTGRES_DB} (${ENV})..."
sleep 1

# --- Run Flyway migration ---
docker run --rm \
  --network bagile-net \
  -v "${MIGRATIONS_DIR}":/flyway/sql \
  flyway/flyway:11.14.0 \
  -url="jdbc:postgresql://bagile-postgres:5432/${POSTGRES_DB}" \
  -user="${POSTGRES_USER}" \
  -password="${POSTGRES_PASSWORD}" \
  -schemas=bagile \
  -locations=filesystem:/flyway/sql \
  -baselineOnMigrate=true \
  migrate

echo "✅ Migrations completed successfully for ${ENV}"
