param(
    [string]$ContainerName = "bagile-db",
    [string]$User = "postgres",
    [string]$Password = "postgres",
    [string]$DbName = "bagile"
)

# Ensure container is running
$containerStatus = docker ps -a --filter "name=$ContainerName" --format "{{.Status}}"

if (-not $containerStatus) {
    Write-Host "Container '$ContainerName' not found. Starting with docker compose..."
    docker compose up -d
} elseif ($containerStatus -notlike "Up*") {
    Write-Host "Container '$ContainerName' exists but not running. Starting..."
    docker start $ContainerName | Out-Null
}

# Export password so psql inside container can use it
$env:PGPASSWORD = $Password

Write-Host "Terminating active connections to $DbName..."
docker exec -e PGPASSWORD=$Password $ContainerName psql -U $User -d postgres -c `
  "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = '$DbName' AND pid <> pg_backend_pid();"

Write-Host "Dropping database $DbName if exists..."
docker exec -e PGPASSWORD=$Password $ContainerName psql -U $User -d postgres -c "DROP DATABASE IF EXISTS $DbName;"


Write-Host "Creating database $DbName..."
docker exec -e PGPASSWORD=$Password $ContainerName psql -U $User -d postgres -c "CREATE DATABASE $DbName;"

Write-Host "Applying migrations..."
Get-ChildItem -Path "migrations" -Filter *.sql |
    Where-Object { $_.Name -ne "999_drop_all.sql" -and $_.Attributes -notmatch "Hidden" } |
    Sort-Object Name | ForEach-Object {
        Write-Host "Running $($_.Name)..."
        docker exec -e PGPASSWORD=$Password $ContainerName psql -U $User -d $DbName -f "/scripts/$($_.Name)"
    }

Write-Host "Database reset complete."
