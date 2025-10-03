param(
    [string]$DbPassword = "bagile123"
)

# Ensure containers are up
docker compose up -d

# Use service name 'postgres' from docker-compose.yml (recommended)
$DbUrl = "jdbc:postgresql://postgres:5432/bagile"
$DbUser = "bagile"

docker run --rm `
  --network bagile-platform_default `
  -v ${PWD}/migrations:/flyway/sql `
  flyway/flyway:latest `
  -url="$DbUrl" `
  -user="$DbUser" `
  -password="$DbPassword" `
  -schemas=bagile `
  repair