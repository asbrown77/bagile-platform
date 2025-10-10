#!/bin/bash
set -e

ENV=${1:-dev}
REPO_DIR=/opt/bagile
MIGRATIONS_DIR="${REPO_DIR}/migrations"
ENV_FILE="${REPO_DIR}/config/bagile.${ENV}.env"

if [ ! -f "$ENV_FILE" ]; then
  echo "❌ Environment file not found: $ENV_FILE"
  exit 1
fi

source "$ENV_FILE"

echo "🚀 Deploying migrations to ${POSTGRES_DB} (${ENV})..."

docker run --rm \
  --network bagile-net \
  -v "${MIGRATIONS_DIR}":/flyway/sql \
  flyway/flyway:latest \
  -url="jdbc:postgresql://bagile-postgres:5432/${POSTGRES_DB}" \
  -user="${POSTGRES_USER}" \
  -password="${POSTGRES_PASSWORD}" \
  -schemas=public \
  -baselineOnMigrate=true \
  migrate
